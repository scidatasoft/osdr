#!/bin/bash

iterations=$1

fileName="FeatureVectors.jmx"

echo "iterations: $iterations"
echo "-------------------------[ $fileName ]-----------------------------"

jmeter -n -t scripts/$fileName -Jusers=$iterations

echo "concat files"
touch stats.csv && find ./results -name '*.csv' | xargs cat >> stats.csv

echo "sort csv"
sort --field-separator=',' -k1 -n stats.csv > target.csv

echo "set header"
sed  -i  '1 i\"index","response code","message error","correlation id","fingerprint","size","radius","total time ms"' target.csv

#count=`cat stats.txt | wc -l`

#sed -i '$count i\""' target.csv

#count=$(($count+1))
#sed  -i  '$count i\"MAX total time","MAX elapsed time"' target.csv