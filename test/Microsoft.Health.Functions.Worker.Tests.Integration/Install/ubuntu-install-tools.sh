#!/bin/bash

dpkg --status azure-functions-core-tools-4 &> /dev/null
if [ $? -eq 0 ]; then
  echo "azure-functions-core-tools-4 is already installed"
else
  DistroVersion=$(cut -f2 <<< "$Var")
  wget -q https://packages.microsoft.com/config/ubuntu/$DistroVersion/packages-microsoft-prod.deb
  sudo dpkg -i packages-microsoft-prod.deb
  sudo apt-get update
  sudo apt-get install azure-functions-core-tools-4
  rm packages-microsoft-prod.deb
fi
