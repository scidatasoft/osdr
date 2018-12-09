#!/bin/bash

iterations=$1

fileName="SSP_Test.jmx"

echo "iterations: $iterations"
echo "-------------------------[ $fileName ]-----------------------------"

jmeter -n -t scripts/$fileName -Jusers=$iterations

echo "concat files"
touch stats.csv && find ./results -name '*.csv' | xargs cat >> stats.csv

echo "sort csv"
#sort -t"," -k1 stats.csv > target.csv
sort --field-separator="\"" -k1 -n stats.csv > target.csv

echo "set header"
sed  -i  '1 i\"session id","model name","total time ms","model id","property","elapsed time ms","result","index","prediction id"' target.csv

#count=`cat stats.txt | wc -l`

#sed -i '$count i\""' target.csv

#count=$(($count+1))
#sed  -i  '$count i\"MAX total time","MAX elapsed time"' target.csv