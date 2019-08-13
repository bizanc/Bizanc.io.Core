#!/bin/bash
mkdir /datadrive/backup 
mv /datadrive/seednode/* /datadrive/backup 
mv /datadrive/backup/RavenDB /datadrive/seednode/RavenDB  
rm -rf /datadrive/backup