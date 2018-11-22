#!/bin/sh

astyle -N -A2 -R "kOS-Mainframe/*.cs"
for i in $(find . -name "*.orig")
do
    rm $i
done