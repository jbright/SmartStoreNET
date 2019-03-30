
REM JAMES: This is the full build process. Just run this.

@echo off
cls
echo Building SmartStore.NET...   											

call build.bat /t:Deploy

RD build\Web\App_Data /S /Q
RD build\Web\Media /S /Q
DEL build\Web\web.config

pause

REM JAMES: Consider adding this to the path in the 
REM JAMES: built.bat file:
REM JAMES: C:\Program Files (x86)\Microsoft Visual Studio\2017\Enterprise\VC\Auxiliary\Build
REM JAMES: That is where the Vars files is.
REM JAMES: using the developer command prompt seems to work fine though.
REM JAMES: yes just open a developer prompt and run the file and your fine.
REM JAMES: Zip the Web folder, copy it to the Mac and upload to the server.