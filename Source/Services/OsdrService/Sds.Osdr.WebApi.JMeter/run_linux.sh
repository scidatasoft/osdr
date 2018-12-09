iteratons=1

newfoldername="$(date +"%y-%m-%d") $@"

mkdir -p ./results/$newfoldername

docker build -t jmeter-base jmeter-base
docker-compose build
docker-compose up  -d

docker cp ./scripts/ master:jmeter/apache-jmeter-4.0/
docker cp ./resources/ master:jmeter/apache-jmeter-4.0/scripts/

for filename in scripts/*.jmx; do
  NAME=$(basename $filename)
  NAME="${NAME%.*}"

  echo "-------------------------[ $NAME ]-----------------------------"

  docker exec -t master /bin/bash -c "jmeter -Jiterations=$iteratons -n -t ./scripts/$NAME.jmx"

  docker cp master:jmeter/apache-jmeter-4.0/results/table.csv "results/$($newfoldername)/$($NAME).jmx_table.csv"
  docker cp master:jmeter/apache-jmeter-4.0/results/tree.csv "results/$($newfoldername)/$($NAME).jmx_tree.csv"

  docker exec -t master /bin/bash -c "rm results/table.csv"
  docker exec -t master /bin/bash -c "rm results/tree.csv"
done;

docker-compose stop && docker-compose rm -f
