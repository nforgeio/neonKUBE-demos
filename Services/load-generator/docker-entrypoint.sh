#!/bin/bash

set PATH=$PATH:/usr/bin

exec dotnet /load-generator/load-generator.dll
