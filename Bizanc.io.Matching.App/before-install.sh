#!/usr/bin/env bash
/bin/bash mkdir /datadrive/backup
/bin/bash mv /datadrive/seednode/* /datadrive/backup
/bin/bash mv /datadrive/backup/RavenDB /datadrive/seednode/RavenDB 
/bin/bash rm -rf /datadrive/backup