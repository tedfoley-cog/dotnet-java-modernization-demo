# java_target — modernization destination (empty skeleton)

This is the **target** of the modernization. It ships as an intentionally empty Spring Boot 3.x /
Java 17 skeleton:

- `pom.xml` — dependencies pre-declared (web, data-jpa, thymeleaf, validation, h2, test)
- `OrderManagementApplication.java` — the `@SpringBootApplication` entry point (only production class)
- `model/`, `repository/`, `service/`, `controller/`, `config/` — **empty** (`.gitkeep`)
- `SmokeTest.java` — placeholder test so the skeleton is green before any migration

**Devin fills these packages in live** during the demo, translating the legacy code under
`../dotnet_source`:

| Legacy .NET (`dotnet_source`) | Migrated Java (here) |
|---|---|
| EF6 entities (`Models/*.cs`) | JPA `@Entity` classes in `model/` |
| `OrderContext : DbContext` | Spring Data `JpaRepository` interfaces in `repository/` |
| `Services/*.cs` (async/await, LINQ) | `@Service` beans in `service/` (Streams, `CompletableFuture`) |
| MVC controllers (`Controllers/*.cs`) | `@Controller` / `@RestController` in `controller/` |
| Razor views (`Views/**/*.cshtml`) | Thymeleaf templates in `src/main/resources/templates/` |

Build locally (after migration, or to verify the skeleton):

```bash
cd java_target
mvn -B compile      # compiles the skeleton
mvn -B test         # runs the placeholder smoke test
```
