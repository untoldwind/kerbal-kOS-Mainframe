#!/bin/sh

msbuild /Property:Configuration=Release

mono packages/NUnit.ConsoleRunner.3.10.0/tools/nunit3-console.exe kOS-Mainframe-Test/bin/Release/kOS-Mainframe-Test.dll
