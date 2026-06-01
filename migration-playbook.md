# Migration Playbook — .NET (C#) → Java (Spring Boot)

This playbook is the **construct-mapping contract** for the live migration. Devin follows it in the
COMPARISON/TRANSLATION phase (Phase 3) and the IMPLEMENTATION phase (Phase 5). Every mapping is taken
from first-party documentation — sources are cited inline so a customer architect can audit them.

## Phase contract (what each phase produces)

| Phase | Devin produces | Reviewed at gate |
|---|---|---|
| 1. Intake | Confirmed scope: one repo, `dotnet_source/` | — |
| 2. Assessment & Analysis | `analysis_output/inventory.json` (files, LOC, constructs, complexity) | **Gate 1** — human reviews assessment |
| 3. Construct Mapping | Filled mapping tables below + `docs/mapping_rules.json` coverage | **Gate 2** — architect approves mapping rules |
| 4. Gap Analysis & Architecture | Target architecture proposal + identified gaps | **Gate 3** — architect signs off |
| 5. Implementation | Java/Spring Boot code in `java_target/` | — |
| 6. Testing & Validation | Generated tests + equivalence results + fix loop | **Gate 4** — human approves PR before merge |
| 7. Output / Release | Modernized app + release/runbook docs | **Gate 5** — human approves release |

## 1. Language & type mappings

| C# construct | Java equivalent | Source |
|---|---|---|
| `namespace X { }` | `package x;` | [C# namespaces](https://learn.microsoft.com/en-us/dotnet/csharp/fundamentals/types/namespaces) / [JLS §7](https://docs.oracle.com/javase/specs/jls/se17/html/jls-7.html) |
| Auto-property `public int Id { get; set; }` | field + getter/setter (or Lombok `@Data`) | [Auto-impl properties](https://learn.microsoft.com/en-us/dotnet/csharp/programming-guide/classes-and-structs/auto-implemented-properties) |
| `decimal` (money) | `java.math.BigDecimal` | [decimal](https://learn.microsoft.com/en-us/dotnet/csharp/language-reference/builtin-types/floating-point-numeric-types) / [BigDecimal](https://docs.oracle.com/javase/8/docs/api/java/math/BigDecimal.html) |
| `DateTime` / `DateTime.UtcNow` | `java.time.LocalDateTime` / `Instant.now()` | [DateTime](https://learn.microsoft.com/en-us/dotnet/api/system.datetime) / [java.time](https://docs.oracle.com/javase/8/docs/api/java/time/package-summary.html) |
| `enum OrderStatus { }` | `enum OrderStatus { }` (+ `@Enumerated`) | [C# enum](https://learn.microsoft.com/en-us/dotnet/csharp/language-reference/builtin-types/enum) |
| `string.IsNullOrWhiteSpace(s)` | `s == null || s.isBlank()` | [String.isBlank](https://docs.oracle.com/en/java/javase/17/docs/api/java.base/java/lang/String.html#isBlank()) |
| `Math.Round(x, 2, MidpointRounding.AwayFromZero)` | `x.setScale(2, RoundingMode.HALF_UP)` | [RoundingMode](https://docs.oracle.com/javase/8/docs/api/java/math/RoundingMode.html) |
| `out` parameter | return a small record/holder (no `out` in Java) | [out params](https://learn.microsoft.com/en-us/dotnet/csharp/language-reference/keywords/out-parameter-modifier) |

## 2. Data access: Entity Framework 6 → Spring Data JPA / Hibernate

| EF6 (C#) | JPA / Spring Data (Java) | Source |
|---|---|---|
| `class OrderContext : DbContext` | `interface OrderRepository extends JpaRepository<Order, Long>` | [Spring Data JPA](https://docs.spring.io/spring-data/jpa/reference/jpa.html) |
| `[Table("Orders")]` | `@Entity @Table(name = "orders")` | [Jakarta Persistence 3.1 §2.1](https://jakarta.ee/specifications/persistence/3.1/) |
| `[Key]` | `@Id @GeneratedValue(strategy = IDENTITY)` | [Jakarta Persistence 3.1 §11.1.17](https://jakarta.ee/specifications/persistence/3.1/) |
| `[Required]` / `[StringLength(120)]` | `@Column(nullable = false, length = 120)` | [Jakarta Persistence 3.1 §11.1.9](https://jakarta.ee/specifications/persistence/3.1/) |
| `[ForeignKey]` + navigation property | `@ManyToOne` / `@OneToMany(mappedBy=...)` | [Jakarta Persistence 3.1 §2.10](https://jakarta.ee/specifications/persistence/3.1/) |
| `DbSet<T>.Add` / `SaveChanges` | `repository.save(entity)` (transactional) | [CrudRepository](https://docs.spring.io/spring-data/commons/docs/current/api/org/springframework/data/repository/CrudRepository.html) |
| `Database.SetInitializer(null)` | `spring.jpa.hibernate.ddl-auto=validate` | [Spring Boot JPA props](https://docs.spring.io/spring-boot/reference/data/sql.html) |
| Stored procedure (`sp_OrderSummary`) | `@Query` (JPQL/native) or projection interface | [Spring Data @Query](https://docs.spring.io/spring-data/jpa/reference/jpa/query-methods.html) |

## 3. LINQ → Java Streams

| LINQ (C#) | Streams (Java) | Source |
|---|---|---|
| `.Where(x => p)` | `.filter(x -> p)` | [Stream.filter](https://docs.oracle.com/javase/8/docs/api/java/util/stream/Stream.html#filter-java.util.function.Predicate-) |
| `.Select(x => f)` | `.map(x -> f)` | [Stream.map](https://docs.oracle.com/javase/8/docs/api/java/util/stream/Stream.html#map-java.util.function.Function-) |
| `.OrderByDescending(x => k)` | `.sorted(comparing(k).reversed())` | [Comparator](https://docs.oracle.com/javase/8/docs/api/java/util/Comparator.html#reversed--) |
| `.First()` / `.FirstOrDefault()` | `.findFirst().orElse(...)` | [Stream.findFirst](https://docs.oracle.com/javase/8/docs/api/java/util/stream/Stream.html#findFirst--) |
| `.Sum(x => x.Amount)` | `.map(...).reduce(ZERO, BigDecimal::add)` | [Stream.reduce](https://docs.oracle.com/javase/8/docs/api/java/util/stream/Stream.html#reduce-T-java.util.function.BinaryOperator-) |
| `.GroupBy(x => k)` | `.collect(groupingBy(k))` | [Collectors.groupingBy](https://docs.oracle.com/javase/8/docs/api/java/util/stream/Collectors.html#groupingBy-java.util.function.Function-) |
| `.ToList()` | `.collect(toList())` / `.toList()` | [Collectors.toList](https://docs.oracle.com/javase/8/docs/api/java/util/stream/Collectors.html#toList--) |

> Note: EF6 async LINQ (`.ToListAsync()`, `.FirstOrDefaultAsync()`) is **server-side** I/O. In Spring
> Data the repository method itself is the boundary; for parallel composition use `CompletableFuture`
> (next section), not `Stream` (sequential by default).

## 4. async/await → CompletableFuture

| C# (TAP) | Java | Source |
|---|---|---|
| `async Task<T> M()` | `CompletableFuture<T> m()` (or `@Async`) | [TAP model](https://learn.microsoft.com/en-us/dotnet/csharp/asynchronous-programming/task-asynchronous-programming-model) / [CompletableFuture](https://docs.oracle.com/javase/8/docs/api/java/util/concurrent/CompletableFuture.html) |
| `await task;` | `future.join()` / `.thenApply(...)` | [CompletionStage](https://docs.oracle.com/javase/8/docs/api/java/util/concurrent/CompletionStage.html) |
| `Task.WhenAll(a, b)` | `CompletableFuture.allOf(a, b)` | [allOf](https://docs.oracle.com/javase/8/docs/api/java/util/concurrent/CompletableFuture.html#allOf-java.util.concurrent.CompletableFuture...-) |
| `Task.FromResult(x)` | `CompletableFuture.completedFuture(x)` | [completedFuture](https://docs.oracle.com/javase/8/docs/api/java/util/concurrent/CompletableFuture.html#completedFuture-U-) |

## 5. ASP.NET MVC → Spring MVC

| ASP.NET MVC (C#) | Spring MVC (Java) | Source |
|---|---|---|
| `class OrdersController : Controller` | `@Controller class OrderController` | [Spring MVC](https://docs.spring.io/spring-framework/reference/web/webmvc/mvc-controller.html) |
| `[HttpGet] ActionResult Index()` | `@GetMapping("/orders") String index(Model m)` | [@GetMapping](https://docs.spring.io/spring-framework/reference/web/webmvc/mvc-controller/ann-requestmapping.html) |
| `[HttpPost] [ValidateAntiForgeryToken]` | `@PostMapping` + Spring Security CSRF | [Spring Security CSRF](https://docs.spring.io/spring-security/reference/servlet/exploits/csrf.html) |
| `[Authorize]` | `@PreAuthorize` / `SecurityFilterChain` | [Method security](https://docs.spring.io/spring-security/reference/servlet/authorization/method-security.html) |
| `return View(model)` | `model.addAttribute(...); return "view";` | [Spring Boot servlet web](https://docs.spring.io/spring-boot/reference/web/servlet.html) |
| `TempData["Message"]` | `RedirectAttributes.addFlashAttribute(...)` | [RedirectAttributes](https://docs.spring.io/spring-framework/reference/web/webmvc/mvc-controller/ann-methods/redirecting-passing-data.html) |

## 6. Razor → Thymeleaf

| Razor (`.cshtml`) | Thymeleaf (`.html`) | Source |
|---|---|---|
| `@model IEnumerable<Order>` | `th:each="o : ${orders}"` | [Thymeleaf iteration](https://www.thymeleaf.org/doc/tutorials/3.1/usingthymeleaf.html#iteration) |
| `@order.OrderNumber` | `th:text="${o.orderNumber}"` | [Thymeleaf text](https://www.thymeleaf.org/doc/tutorials/3.1/usingthymeleaf.html#standard-expression-syntax) |
| `@Html.ActionLink(...)` | `th:href="@{/orders/{id}(id=${o.id})}"` | [Thymeleaf URLs](https://www.thymeleaf.org/doc/tutorials/3.1/usingthymeleaf.html#link-urls) |
| `_Layout.cshtml` + `@RenderBody()` | `th:fragment` / layout dialect | [Thymeleaf layouts](https://www.thymeleaf.org/doc/tutorials/3.1/usingthymeleaf.html#template-layout) |
| `@if (cond) { }` | `th:if="${cond}"` | [Thymeleaf conditionals](https://www.thymeleaf.org/doc/tutorials/3.1/usingthymeleaf.html#conditional-evaluation) |

## 7. Equivalence rules (Phase 6)

The migrated Java must reproduce the legacy behavior exactly for these golden cases (the credit-check
and pricing logic are the highest-value rules):

- **Volume discount** — same fraction per `(tier, quantity)` band as `PricingService.GetDiscountRate`.
- **Tax** — same per `(taxClass, region)` as `PricingService.CalculateTax` (EXM=0, RED=5%, STD by region).
- **Rounding** — 2 dp, half-away-from-zero (`RoundingMode.HALF_UP` on `BigDecimal`).
- **Credit check** — block when `outstanding_unpaid + grand_total > credit_limit` or `OnCreditHold`.
- **Invoice numbering** — `INV-{year}-{0000}` monotonic per year.
