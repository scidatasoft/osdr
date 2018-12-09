FROM jboss/keycloak:3.4.3.Final

RUN mkdir -p settings
COPY keycloak-settings/osdr-realm.json ./settings

CMD ["-b", "0.0.0.0", "-bmanagement", "0.0.0.0", "-Dkeycloak.import=settings/osdr-realm.json"]