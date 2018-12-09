FROM openjdk:8-jre

RUN mkdir /jmeter \
    && cd /jmeter/ \
    && wget https://archive.apache.org/dist/jmeter/binaries/apache-jmeter-4.0.tgz \
    && tar -xvzf apache-jmeter-4.0.tgz \
    && rm apache-jmeter-4.0.tgz

ENV JMETER_HOME /jmeter/apache-jmeter-4.0/

# Add Jmeter to the Path
ENV PATH $JMETER_HOME/bin:$PATH

WORKDIR $JMETER_HOME
# Ports to be exposed from the container for JMeter Master
RUN mkdir scripts && mkdir results

COPY . .

EXPOSE 60000
