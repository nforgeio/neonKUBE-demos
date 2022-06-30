@echo off
REM Configures the environment variables required to build neonKUBE projects.
REM 
REM 	buildenv [ <source folder> ]
REM
REM Note that <source folder> defaults to the folder holding this
REM batch file.
REM
REM This must be [RUN AS ADMINISTRATOR].

echo ===========================================
echo * neonKUBE Build Environment Configurator *
echo ===========================================

REM Default NF_DEMO_ROOT to the folder holding this batch file after stripping
REM off the trailing backslash.

set NF_DEMO_ROOT=%~dp0 
set NF_DEMO_ROOT=%NF_DEMO_ROOT:~0,-2%
setx NF_DEMO_ROOT "%NF_DEMO_ROOT%" /M                                   > nul

if not [%1]==[] set NF_DEMO_ROOT=%1

if exist %NF_DEMO_ROOT%\neonKUBE-Demos.sln goto goodPath
echo The [%NF_DEMO_ROOT%\neonKUBE-Demos.sln] file does not exist.  Please pass the path
echo to the neonKUBE solution folder.
goto done

:goodPath
