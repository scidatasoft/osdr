#!/bin/bash

iterations=$1

newfoldername="$(date +"%y-%m-%d")"

function build() {
	echo "---------- build ----------"

	docker-compose build
}

function start() {
	echo "---------- start ----------"

	docker-compose up  -d

	docker exec -it jmeter /bin/bash -c "sh vf1.sh $iterations";

	mkdir ./results
	mkdir ./results/$newfoldername

	echo "copy report on local host:"

	docker cp jmeter:jmeter/apache-jmeter-4.0/target.csv "results/$newfoldername/fv_stats.csv"
	docker cp jmeter:jmeter/apache-jmeter-4.0/jmeter.log "results/$newfoldername/jmeter.log"

#	echo "Calculating average report..."

#	dotnet CsvExtractor.SSP.dll ./results/$newfoldername/ssp_stats.csv

	echo "DONE"
	docker-compose stop
	docker-compose rm -f
}

if [ -n "$2" ]
then
	echo
	while [ -n "$2" ]
	do
	case "$2" in
	-build) build ;;
	-start) start ;;
	*) echo "$2 is not an option" ;;
	esac
	shift
	done
else
echo "No parameters found. Select -build or -start"
fi
