#!/bin/bash
dotnet publish -c Release
./bin/Release/net6.0/publish/ADAPH.TxSubmit --urls "http://localhost:1337"