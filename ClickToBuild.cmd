
REM JAMES: This is the full build process. Just run this.

@echo off
cls
echo Building SmartStore.NET...   											

call build.bat /t:Deploy

RD build\Web\App_Data /S /Q
RD build\Web\Media /S /Q
DEL build\Web\web.config

pause