package com.acme.ordermgmt;

import static org.assertj.core.api.Assertions.assertThat;

import java.io.IOException;
import java.io.InputStream;
import java.sql.Connection;
import java.sql.SQLException;
import java.util.Properties;

import javax.sql.DataSource;

import org.junit.jupiter.api.Nested;
import org.junit.jupiter.api.Test;
import org.springframework.beans.factory.annotation.Autowired;
import org.springframework.boot.test.context.SpringBootTest;
import org.springframework.test.context.ActiveProfiles;

/**
 * Guards the datasource externalisation: the default profile takes every
 * connection setting from the environment, and only the local H2 "dev"
 * profile carries committed values.
 */
class DatasourceConfigTest {

    @Test
    void defaultProfileReadsDatasourceOnlyFromEnvironment() throws IOException {
        Properties props = new Properties();
        try (InputStream in = getClass().getResourceAsStream("/application.properties")) {
            props.load(in);
        }
        assertThat(props.getProperty("spring.datasource.url")).isEqualTo("${SPRING_DATASOURCE_URL}");
        assertThat(props.getProperty("spring.datasource.username")).isEqualTo("${SPRING_DATASOURCE_USERNAME}");
        assertThat(props.getProperty("spring.datasource.password")).isEqualTo("${SPRING_DATASOURCE_PASSWORD}");
    }

    @Nested
    @SpringBootTest
    @ActiveProfiles("dev")
    class DevProfile {

        @Autowired
        DataSource dataSource;

        @Test
        void usesInMemoryH2() throws SQLException {
            try (Connection connection = dataSource.getConnection()) {
                assertThat(connection.getMetaData().getURL()).startsWith("jdbc:h2:mem:");
            }
        }
    }
}
