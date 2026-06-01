package com.acme.ordermgmt;

import org.springframework.boot.SpringApplication;
import org.springframework.boot.autoconfigure.SpringBootApplication;

/**
 * Entry point for the modernized Order Management service.
 *
 * <p>This is the ONLY production class in the skeleton. The {@code model},
 * {@code repository}, {@code service}, and {@code controller} packages are empty —
 * Devin migrates the legacy .NET logic from {@code ../dotnet_source} into them
 * live during the demo (EF entities &rarr; JPA {@code @Entity}, services &rarr;
 * {@code @Service}, MVC controllers &rarr; Spring {@code @Controller}/{@code @RestController}).
 */
@SpringBootApplication
public class OrderManagementApplication {

    public static void main(String[] args) {
        SpringApplication.run(OrderManagementApplication.class, args);
    }
}
