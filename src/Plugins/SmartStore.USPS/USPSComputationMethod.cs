﻿
//------------------------------------------------------------------------------
// Contributor(s): RJH 08/07/2009, mb 10/20/2010, AC 05/16/2011.
//------------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Net;
using System.Text;
using System.Web.Routing;
using System.Xml;
using SmartStore.Core.Domain.Directory;
using SmartStore.Core.Domain.Shipping;
using SmartStore.Core.Localization;
using SmartStore.Core.Plugins;
using SmartStore.Services;
using SmartStore.Services.Catalog;
using SmartStore.Services.Configuration;
using SmartStore.Services.Directory;
using SmartStore.Services.Shipping;
using SmartStore.Services.Shipping.Tracking;
using SmartStore.USPS.Domain;

namespace SmartStore.USPS
{
	/// <summary>
	/// USPS computation method
	/// </summary>
	public class USPSComputationMethod : BasePlugin, IShippingRateComputationMethod, IConfigurable
    {
        #region Constants

        private const int MAXPACKAGEWEIGHT = 70;
        private const string MEASUREWEIGHTSYSTEMKEYWORD = "oz";
        private const string MEASUREDIMENSIONSYSTEMKEYWORD = "inch";

        #endregion

        #region Fields

        private readonly IMeasureService _measureService;
        private readonly IShippingService _shippingService;
        private readonly ISettingService _settingService;
        private readonly USPSSettings _uspsSettings;
        private readonly MeasureSettings _measureSettings;
        private readonly IPriceCalculationService _priceCalculationService;
        private readonly ICommonServices _services;

        #endregion

        #region Ctor

        public USPSComputationMethod(IMeasureService measureService,
            IShippingService shippingService, ISettingService settingService,
            USPSSettings uspsSettings, MeasureSettings measureSettings,
            IPriceCalculationService priceCalculationService, ICommonServices services)
        {
            this._measureService = measureService;
            this._shippingService = shippingService;
            this._settingService = settingService;
            this._uspsSettings = uspsSettings;
            this._measureSettings = measureSettings;
            this._priceCalculationService = priceCalculationService;
            this._services = services;

			T = NullLocalizer.Instance;
		}

		public Localizer T { get; set; }

		#endregion

		#region Utilities

		/// <summary>
		/// Convert pounds to ounces
		/// </summary>
		/// <param name="pounds"></param>
		/// <returns></returns>
		protected int PoundsToOunces(int pounds)
        {
            return Convert.ToInt32(pounds * 16M);
        }

        /// <summary>
        /// Is a request domestic
        /// </summary>
        /// <param name="getShippingOptionRequest">Request</param>
        /// <returns>Rsult</returns>
        protected bool IsDomesticRequest(GetShippingOptionRequest getShippingOptionRequest)
        {
            //Origin Country must be USA, Collect USA from list of countries
            bool result = true;
            if (getShippingOptionRequest != null &&
                getShippingOptionRequest.ShippingAddress != null &&
                getShippingOptionRequest.ShippingAddress.Country != null)
            {
                result = getShippingOptionRequest.ShippingAddress.Country.ThreeLetterIsoCode == "USA";
            }
            return result;
        }

        private string CreateRequest(string username, string password, GetShippingOptionRequest getShippingOptionRequest)
        {
            var usedMeasureWeight = _measureService.GetMeasureWeightBySystemKeyword(MEASUREWEIGHTSYSTEMKEYWORD);
            if (usedMeasureWeight == null)
                throw new SmartException(string.Format("USPS shipping service. Could not load \"{0}\" measure weight", MEASUREWEIGHTSYSTEMKEYWORD));

            var baseusedMeasureWeight = _measureService.GetMeasureWeightById(_measureSettings.BaseWeightId);
            if (baseusedMeasureWeight == null)
                throw new SmartException("Primary weight can't be loaded");
            
            var usedMeasureDimension = _measureService.GetMeasureDimensionBySystemKeyword(MEASUREDIMENSIONSYSTEMKEYWORD);
            if (usedMeasureDimension == null)
                throw new SmartException(string.Format("USPS shipping service. Could not load \"{0}\" measure dimension", MEASUREDIMENSIONSYSTEMKEYWORD));

            var baseusedMeasureDimension = _measureService.GetMeasureDimensionById(_measureSettings.BaseDimensionId);
            if (usedMeasureDimension == null)
                throw new SmartException("Primary dimension can't be loaded");


            int length = Convert.ToInt32(Math.Ceiling(getShippingOptionRequest.GetTotalLength() / baseusedMeasureDimension.Ratio * usedMeasureDimension.Ratio));
            int height = Convert.ToInt32(Math.Ceiling(getShippingOptionRequest.GetTotalHeight() / baseusedMeasureDimension.Ratio * usedMeasureDimension.Ratio));
            int width = Convert.ToInt32(Math.Ceiling(getShippingOptionRequest.GetTotalWidth() / baseusedMeasureDimension.Ratio * usedMeasureDimension.Ratio));
            int weight = Convert.ToInt32(Math.Ceiling(_shippingService.GetShoppingCartTotalWeight(getShippingOptionRequest.Items) / baseusedMeasureWeight.Ratio * usedMeasureWeight.Ratio));
            
            if (length < 1) length = 1;
            if (height < 1) height = 1;
            if (width < 1) width = 1;
            if (weight < 1) weight = 1;

            string zipPostalCodeFrom = getShippingOptionRequest.ZipPostalCodeFrom;
            string zipPostalCodeTo = getShippingOptionRequest.ShippingAddress.ZipPostalCode;

            //valid values for testing. http://testing.shippingapis.com/ShippingAPITest.dll
            //Zip to = "20008"; Zip from ="10022"; weight = 2;

            int pounds = Convert.ToInt32(weight / 16);
            int ounces = Convert.ToInt32(weight - (pounds * 16.0M));
            int girth = height + height + width + width;
            //Get shopping cart sub-total.  V2 International rates require the package value to be declared.
            decimal subTotal = decimal.Zero;
            foreach (var shoppingCartItem in getShippingOptionRequest.Items)
            {
                if (!shoppingCartItem.Item.IsShipEnabled)
                    continue;
                subTotal += _priceCalculationService.GetSubTotal(shoppingCartItem, true);
            }

            string requestString = string.Empty;

            bool isDomestic = IsDomesticRequest(getShippingOptionRequest);
            if (isDomestic)
            {
                #region domestic request
				zipPostalCodeFrom = zipPostalCodeFrom.Truncate(5).EnsureNumericOnly();
				zipPostalCodeTo = zipPostalCodeTo.Truncate(5).EnsureNumericOnly();

                var sb = new StringBuilder();
                sb.AppendFormat("<RateV4Request USERID=\"{0}\" PASSWORD=\"{1}\">", username, password);
                sb.Append("<Revision>2</Revision>");

                var xmlStrings = new USPSStrings(); // Create new instance with string array

                if ((!IsPackageTooHeavy(pounds)) && (!IsPackageTooLarge(length, height, width)))
                {
                    var packageSize = GetPackageSize(length, height, width);
                    // RJH get all XML strings not commented out for USPSStrings. 
                    // RJH V3 USPS Service must be Express, Express SH, Express Commercial, Express SH Commercial, First Class, Priority, Priority Commercial, Parcel, Library, BPM, Media, ALL or ONLINE;
                    // AC - Updated to V4 API and made minor improvements to allow First Class Packages (package only - not envelopes).


                    foreach (string element in xmlStrings.Elements) // Loop over elements with property
                    {
                        if ((element == "First Class") && (weight >= 14))
                        {
                            // AC - At the time of coding there aren't any First Class shipping options for packages over 13 ounces. 
                        }
                        else
                        {
                            sb.Append("<Package ID=\"0\">");

                            sb.AppendFormat("<Service>{0}</Service>", element);
                            if (element == "First Class")
                                sb.Append("<FirstClassMailType>PARCEL</FirstClassMailType>");

                            sb.AppendFormat("<ZipOrigination>{0}</ZipOrigination>", zipPostalCodeFrom);
                            sb.AppendFormat("<ZipDestination>{0}</ZipDestination>", zipPostalCodeTo);
                            sb.AppendFormat("<Pounds>{0}</Pounds>", pounds);
                            sb.AppendFormat("<Ounces>{0}</Ounces>", ounces);
                            sb.Append("<Container/>");
                            sb.AppendFormat("<Size>{0}</Size>", packageSize);
                            sb.AppendFormat("<Width>{0}</Width>", width);
                            sb.AppendFormat("<Length>{0}</Length>", length);
                            sb.AppendFormat("<Height>{0}</Height>", height);
                            sb.AppendFormat("<Girth>{0}</Girth>", girth);

                            sb.Append("<Machinable>FALSE</Machinable>");

                            sb.Append("</Package>");
                        }
                    }
                }
                else
                {
                    int totalPackages = 1;
                    int totalPackagesDims = 1;
                    int totalPackagesWeights = 1;
                    if (IsPackageTooHeavy(pounds))
                    {
                        totalPackagesWeights = Convert.ToInt32(Math.Ceiling((decimal)pounds / (decimal)MAXPACKAGEWEIGHT));
                    }
                    if (IsPackageTooLarge(length, height, width))
                    {
                        totalPackagesDims = Convert.ToInt32(Math.Ceiling((decimal)TotalPackageSize(length, height, width) / (decimal)108));
                    }
                    totalPackages = totalPackagesDims > totalPackagesWeights ? totalPackagesDims : totalPackagesWeights;
                    if (totalPackages == 0)
                        totalPackages = 1;

                    int pounds2 = pounds / totalPackages;
                    //we don't use ounces
                    int ounces2 = ounces / totalPackages;
                    int height2 = height / totalPackages;
                    int width2 = width / totalPackages;
                    int length2 = length / totalPackages;

                    if (pounds2 < 1) pounds2 = 1;
                    if (height2 < 1) height2 = 1;
                    if (width2 < 1) width2 = 1;
                    if (length2 < 1) length2 = 1;

                    var packageSize = GetPackageSize(length2, height2, width2);
                    
                    int girth2 = height2 + height2 + width2 + width2;

                    for (int i = 0; i < totalPackages; i++)
                    {
                        foreach (string element in xmlStrings.Elements)
                        {
                            if ((element == "First Class") && (weight >= 14))
                            {
                                // AC - At the time of coding there aren't any First Class shipping options for packages over 13 ounces. 
                            }
                            else
                            {
                                sb.AppendFormat("<Package ID=\"{0}\">", i.ToString());
                                sb.AppendFormat("<Service>{0}</Service>", element);
                                if (element == "First Class")
                                    sb.Append("<FirstClassMailType>PARCEL</FirstClassMailType>");
                                sb.AppendFormat("<ZipOrigination>{0}</ZipOrigination>", zipPostalCodeFrom);
                                sb.AppendFormat("<ZipDestination>{0}</ZipDestination>", zipPostalCodeTo);
                                sb.AppendFormat("<Pounds>{0}</Pounds>", pounds2);
                                sb.AppendFormat("<Ounces>{0}</Ounces>", ounces2);
                                sb.Append("<Container/>");
                                sb.AppendFormat("<Size>{0}</Size>", packageSize);
                                sb.AppendFormat("<Width>{0}</Width>", width2);
                                sb.AppendFormat("<Length>{0}</Length>", length2);
                                sb.AppendFormat("<Height>{0}</Height>", height2);
                                sb.AppendFormat("<Girth>{0}</Girth>", girth2);
                                sb.Append("<Machinable>FALSE</Machinable>");
                                sb.Append("</Package>");
                            }
                        }
                    }
                }

                sb.Append("</RateV4Request>");

                requestString = "API=RateV4&XML=" + sb.ToString();
                #endregion
            }
            else
            {
                #region international request
                var sb = new StringBuilder();
                // sb.AppendFormat("<IntlRateRequest USERID=\"{0}\" PASSWORD=\"{1}\">", username, password);
                sb.AppendFormat("<IntlRateV2Request USERID=\"{0}\" PASSWORD=\"{1}\">", username, password);
                sb.AppendFormat("<Revision>2</Revision>");

                //V2 International rates require the package value to be declared.  Max content value for most shipping options is $400 so it is limited here.  
                decimal intlSubTotal = decimal.Zero;
                if (subTotal > 400)
                {
                    intlSubTotal = 400;
                }
                else
                {
                    intlSubTotal = subTotal;
                }

                //little hack here for international requests
                length = 12;
                width = 12;
                height = 12;
                girth = height + height + width + width;

                string mailType = "Package"; //Package, Envelope
                var packageSize = GetPackageSize(length, height, width);

                if ((!IsPackageTooHeavy(pounds)) && (!IsPackageTooLarge(length, height, width)))
                {
                    sb.Append("<Package ID=\"0\">");
                    sb.AppendFormat("<Pounds>{0}</Pounds>", pounds);
                    sb.AppendFormat("<Ounces>{0}</Ounces>", ounces);
                    sb.Append("<Machinable>FALSE</Machinable>");
                    sb.AppendFormat("<MailType>{0}</MailType>", mailType);
                    sb.Append("<GXG>");
                    sb.Append("<POBoxFlag>N</POBoxFlag>");
                    sb.Append("<GiftFlag>N</GiftFlag>");
                    sb.Append("</GXG>");
                    sb.AppendFormat("<ValueOfContents>{0}</ValueOfContents>", intlSubTotal);
                    sb.AppendFormat("<Country>{0}</Country>", getShippingOptionRequest.ShippingAddress.Country.Name);
                    sb.Append("<Container>RECTANGULAR</Container>");
                    sb.AppendFormat("<Size>{0}</Size>", packageSize);
                    sb.AppendFormat("<Width>{0}</Width>", width);
                    sb.AppendFormat("<Length>{0}</Length>", length);
                    sb.AppendFormat("<Height>{0}</Height>", height);
                    sb.AppendFormat("<Girth>{0}</Girth>", girth);
                    sb.AppendFormat("<OriginZip>{0}</OriginZip>", zipPostalCodeFrom);
                    sb.Append("<CommercialFlag>N</CommercialFlag>");
                    sb.Append("</Package>");
                }
                else
                {
                    int totalPackages = 1;
                    int totalPackagesDims = 1;
                    int totalPackagesWeights = 1;
                    if (IsPackageTooHeavy(pounds))
                    {
                        totalPackagesWeights = Convert.ToInt32(Math.Ceiling((decimal)pounds / (decimal)MAXPACKAGEWEIGHT));
                    }
                    if (IsPackageTooLarge(length, height, width))
                    {
                        totalPackagesDims = Convert.ToInt32(Math.Ceiling((decimal)TotalPackageSize(length, height, width) / (decimal)108));
                    }
                    totalPackages = totalPackagesDims > totalPackagesWeights ? totalPackagesDims : totalPackagesWeights;
                    if (totalPackages == 0)
                        totalPackages = 1;

                    int pounds2 = pounds / totalPackages;
                    //we don't use ounces
                    int ounces2 = ounces / totalPackages;
                    int height2 = height / totalPackages;
                    int width2 = width / totalPackages;
                    int length2 = length / totalPackages;
                    if (pounds2 < 1)
                        pounds2 = 1;
                    if (height2 < 1)
                        height2 = 1;
                    if (width2 < 1)
                        width2 = 1;
                    if (length2 < 1)
                        length2 = 1;

                    //little hack here for international requests
                    length2 = 12;
                    width2 = 12;
                    height2 = 12;
                    var packageSize2 = GetPackageSize(length2, height2, width2);
                    int girth2 = height2 + height2 + width2 + width2;

                    for (int i = 0; i < totalPackages; i++)
                    {
                        sb.AppendFormat("<Package ID=\"{0}\">", i.ToString());
                        sb.AppendFormat("<Pounds>{0}</Pounds>", pounds2);
                        sb.AppendFormat("<Ounces>{0}</Ounces>", ounces2);
                        sb.Append("<Machinable>FALSE</Machinable>");
                        sb.AppendFormat("<MailType>{0}</MailType>", mailType);
                        sb.Append("<GXG>");
                        sb.Append("<POBoxFlag>N</POBoxFlag>");
                        sb.Append("<GiftFlag>N</GiftFlag>");
                        sb.Append("</GXG>");
                        sb.AppendFormat("<ValueOfContents>{0}</ValueOfContents>", intlSubTotal);
                        sb.AppendFormat("<Country>{0}</Country>", getShippingOptionRequest.ShippingAddress.Country.Name);
                        sb.Append("<Container>RECTANGULAR</Container>");
                        sb.AppendFormat("<Size>{0}</Size>", packageSize2);
                        sb.AppendFormat("<Width>{0}</Width>", width2);
                        sb.AppendFormat("<Length>{0}</Length>", length2);
                        sb.AppendFormat("<Height>{0}</Height>", height2);
                        sb.AppendFormat("<Girth>{0}</Girth>", girth2);
                        sb.Append("<CommercialFlag>N</CommercialFlag>");
                        sb.Append("</Package>");
                    }
                }

                sb.Append("</IntlRateV2Request>");

                requestString = "API=IntlRateV2&XML=" + sb.ToString();
                #endregion
            }

            return requestString;
        }

        private string DoRequest(string url, string requestString)
        {
            byte[] bytes = new ASCIIEncoding().GetBytes(requestString);
            var request = (HttpWebRequest)WebRequest.Create(url);
            request.Method = "POST";
            request.ContentType = "application/x-www-form-urlencoded";
            request.ContentLength = bytes.Length;
            var requestStream = request.GetRequestStream();
            requestStream.Write(bytes, 0, bytes.Length);
            requestStream.Close();
            var response = request.GetResponse();
            string responseXML = string.Empty;
            using (var reader = new StreamReader(response.GetResponseStream()))
                responseXML = reader.ReadToEnd();

            return responseXML;
        }

        private bool IsPackageTooLarge(int length, int height, int width)
        {
            int total = TotalPackageSize(length, height, width);
            if (total > 130)
                return true;
            else
                return false;
        }

        private int TotalPackageSize(int length, int height, int width)
        {
            int girth = height + height + width + width;
            int total = girth + length;
            return total;
        }

        private bool IsPackageTooHeavy(int weight)
        {
            if (weight > MAXPACKAGEWEIGHT)
                return true;
            else
                return false;
        }

        private USPSPackageSize GetPackageSize(int length, int height, int width)
        {
            int girth = height + height + width + width;
            int total = girth + length;
            if (total <= 84)
                return USPSPackageSize.Regular;
            if ((total > 84) && (total <= 108))
                return USPSPackageSize.Large;
            if ((total > 108) && (total <= 130))
                return USPSPackageSize.Oversize;
            else
                throw new SmartException("Shipping Error: Package too large.");
        }

        private List<ShippingOption> ParseResponse(string response, bool isDomestic, ref string error)
        {
            var shippingOptions = new List<ShippingOption>();

            string postageStr = isDomestic ? "Postage" : "Service";
            string mailServiceStr = isDomestic ? "MailService" : "SvcDescription";
            string rateStr = isDomestic ? "Rate" : "Postage";
            string classStr = isDomestic ? "CLASSID" : "ID";
            string carrierServicesOffered = isDomestic ? _uspsSettings.CarrierServicesOfferedDomestic : _uspsSettings.CarrierServicesOfferedInternational;

            using (var sr = new StringReader(response))
            using (var tr = new XmlTextReader(sr))
            {
                do
                {
                    tr.Read();

                    if ((tr.Name == "Error") && (tr.NodeType == XmlNodeType.Element))
                    {
                        string errorText = "";
                        while (tr.Read())
                        {
                            if ((tr.Name == "Description") && (tr.NodeType == XmlNodeType.Element))
                                errorText += "Error Desc: " + tr.ReadString();
                            if ((tr.Name == "HelpContext") && (tr.NodeType == XmlNodeType.Element))
                                errorText += "USPS Help Context: " + tr.ReadString() + ". ";
                        }
                        error = "USPS Error returned: " + errorText;
                    }

                    if ((tr.Name == postageStr) && (tr.NodeType == XmlNodeType.Element))
                    {
                        string serviceId = string.Empty;

                        // Find the ID for the service
                        if (tr.HasAttributes)
                        {
                            for (int i = 0; i < tr.AttributeCount; i++)
                            {
                                tr.MoveToAttribute(i);
                                if (tr.Name.Equals(classStr))
                                {
                                    // Add delimiters [] so that single digit IDs aren't found in mutli-digit IDs                                    
                                    serviceId = String.Format("[{0}]", tr.Value);
                                    break;
                                }
                            }
                        }

                        // Go to the next rate if the service ID is not in the list of services to offer
                        if (!String.IsNullOrEmpty(serviceId) &&
                            !String.IsNullOrEmpty(carrierServicesOffered) &&
                            !carrierServicesOffered.Contains(serviceId))
                        {
                            continue;
                        }

                        string serviceCode = string.Empty;
                        string postalRate = string.Empty;

                        do
                        {

                            tr.Read();

                            if ((tr.Name == mailServiceStr) && (tr.NodeType == XmlNodeType.Element))
                            {
                                serviceCode = tr.ReadString();

                                tr.ReadEndElement();
                                if ((tr.Name == mailServiceStr) && (tr.NodeType == XmlNodeType.EndElement))
                                    break;
                            }

                            if ((tr.Name == rateStr) && (tr.NodeType == XmlNodeType.Element))
                            {
                                postalRate = tr.ReadString();
                                tr.ReadEndElement();
                                if ((tr.Name == rateStr) && (tr.NodeType == XmlNodeType.EndElement))
                                    break;
                            }

                        }
                        while (!((tr.Name == postageStr) && (tr.NodeType == XmlNodeType.EndElement)));


                        //USPS issue fixed
                        char tm = (char)174;
                        serviceCode = serviceCode.Replace("&lt;sup&gt;&amp;reg;&lt;/sup&gt;", tm.ToString());

                        ShippingOption shippingOption = shippingOptions.Find((s) => s.Name == serviceCode);
                        if (shippingOption == null)
                        {
                            shippingOption = new ShippingOption();
                            shippingOption.Name = serviceCode;
                            shippingOptions.Add(shippingOption);
                        }
                        shippingOption.Rate += Convert.ToDecimal(postalRate, new CultureInfo("en-US"));
                    }
                }
                while (!tr.EOF);
            }
            return shippingOptions;
        }

        #endregion

        #region Methods

        /// <summary>
        ///  Gets available shipping options
        /// </summary>
        /// <param name="getShippingOptionRequest">A request for getting shipping options</param>
        /// <returns>Represents a response of getting shipping rate options</returns>
        public GetShippingOptionResponse GetShippingOptions(GetShippingOptionRequest getShippingOptionRequest)
        {
            if (getShippingOptionRequest == null)
                throw new ArgumentNullException("getShippingOptionRequest");

            var response = new GetShippingOptionResponse();

            if (getShippingOptionRequest.Items == null)
            {
                response.AddError(T("Admin.System.Warnings.NoShipmentItems"));
                return response;
            }

            if (getShippingOptionRequest.ShippingAddress == null)
            {
                return response;
            }

            if (String.IsNullOrEmpty(getShippingOptionRequest.ZipPostalCodeFrom))
                getShippingOptionRequest.ZipPostalCodeFrom = _uspsSettings.ZipPostalCodeFrom;


            bool isDomestic = IsDomesticRequest(getShippingOptionRequest);
            string requestString = CreateRequest(_uspsSettings.Username, _uspsSettings.Password, getShippingOptionRequest);
            string responseXml = DoRequest(USPSHelper.GetApiUrl(_uspsSettings.UseSandbox), requestString);
            string error = "";
            var shippingOptions = ParseResponse(responseXml, isDomestic, ref error);
            if (String.IsNullOrEmpty(error))
            {
                foreach (var shippingOption in shippingOptions)
                {
                    if (!shippingOption.Name.ToLower().StartsWith("usps"))
                        shippingOption.Name = string.Format("USPS {0}", shippingOption.Name);
                    shippingOption.Rate += _uspsSettings.AdditionalHandlingCharge;
                    response.ShippingOptions.Add(shippingOption);
                }
            }
            else
            {
                response.AddError(error);
            }

            return response;
        }

        /// <summary>
        /// Gets fixed shipping rate (if shipping rate computation method allows it and the rate can be calculated before checkout).
        /// </summary>
        /// <param name="getShippingOptionRequest">A request for getting shipping options</param>
        /// <returns>Fixed shipping rate; or null in case there's no fixed shipping rate</returns>
        public decimal? GetFixedRate(GetShippingOptionRequest getShippingOptionRequest)
        {
            return null;
        }

        /// <summary>
        /// Gets a route for provider configuration
        /// </summary>
        /// <param name="actionName">Action name</param>
        /// <param name="controllerName">Controller name</param>
        /// <param name="routeValues">Route values</param>
        public void GetConfigurationRoute(out string actionName, out string controllerName, out RouteValueDictionary routeValues)
        {
            actionName = "Configure";
            controllerName = "USPS";
            routeValues = new RouteValueDictionary() { { "area", "SmartStore.USPS" } };
        }
        
        /// <summary>
        /// Install plugin
        /// </summary>
        public override void Install()
        {
            //settings
            var settings = new USPSSettings()
            {
                UseSandbox = true,
                Username = "123",
                Password = "456",
                AdditionalHandlingCharge = 0,
                ZipPostalCodeFrom = "10022",
                CarrierServicesOfferedDomestic = "",
                CarrierServicesOfferedInternational = ""
            };
            _settingService.SaveSetting(settings);


            //locales
            _services.Localization.ImportPluginResourcesFromXml(this.PluginDescriptor);
            
            base.Install();
        }

        /// <summary>
        /// Uninstall plugin
        /// </summary>
        public override void Uninstall()
        {
            //locales
            _services.Localization.DeleteLocaleStringResources(this.PluginDescriptor.ResourceRootKey);
            _services.Localization.DeleteLocaleStringResources("Plugins.FriendlyName.Shipping.UPS", false);

            base.Uninstall();
        }

        #endregion

        #region Properties

        /// <summary>
        /// Gets a shipping rate computation method type
        /// </summary>
        public ShippingRateComputationMethodType ShippingRateComputationMethodType
        {
            get
            {
                return ShippingRateComputationMethodType.Realtime;
            }
        }
        
        /// <summary>
        /// Gets a shipment tracker
        /// </summary>
        public IShipmentTracker ShipmentTracker
        {
            get { return null; }
        }

		public bool IsActive
		{
			get { return true; }
		}

        #endregion
    }
}