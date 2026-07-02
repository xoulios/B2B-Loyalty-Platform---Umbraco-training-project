# VideoClubV2 — Υλοποιημένα Technologies & Concepts (μπούσουλα)

Αναλυτικός κατάλογος όλων όσων υλοποιήθηκαν στο project (Plan B: Rich Domain + Service Layer), για χρήση ως οδηγός σε **B2B loyalty platform με Clean Architecture σε Umbraco 17 LTS**.

- **Stack:** .NET 8, C# 12, ASP.NET Core MVC, EF Core 8 (SQL Server / LocalDB), ASP.NET Core Identity, AutoMapper, Serilog, xUnit.
- **Solution:** `VideoClubV2.slnx` — 4 projects: `VideoClub.Core`, `VideoClub.Infrastructure`, `VideoClub.Web`, `VideoClub.Tests`.
- **Φιλοσοφία:** layered / onion-lite με dependency inversion· rich domain entities· thin controllers· services για orchestration.

---

## 1. Αρχιτεκτονική & δομή

| Concept | Πού | Σημείωση |
|---|---|---|
| **Layered architecture** (Core / Infrastructure / Web) | 3 projects | Διαχωρισμός responsibilities |
| **Dependency rule** | `Core` δεν εξαρτάται από τίποτα· `Infrastructure → Core`· `Web → Infrastructure + Core` | Εξαρτήσεις δείχνουν προς τα μέσα |
| **Dependency Inversion** | Interfaces στο `Core/Services`, implementations στο `Infrastructure/Services` | Το Web εξαρτάται από abstractions |
| **Composition Root** | `Program.cs` (το μόνο σημείο που «ξέρει» όλα τα layers) | Wiring μόνο εδώ |
| **DI registration extension** | `Infrastructure/DependencyInjection/InfrastructureServiceRegistration.AddInfrastructure()` | Κρατά καθαρό το `Program.cs` |
| **Module separation με MVC Areas** | `Web/Areas/Admin/...` | Admin module ξεχωριστά από customer-facing |

**Βασικός κανόνας που τηρήθηκε:** εξαρτήσου από abstractions παντού — εκτός από το composition root, που επιτρέπεται να γνωρίζει τα concrete.

---

## 2. Domain layer (`VideoClub.Core`) — Rich Domain Model

Το «καρδιά» του Plan B: **όχι anemic models**. Τα entities κρατούν δεδομένα *και* συμπεριφορά + invariants.

| Concept | Πού | Λεπτομέρεια |
|---|---|---|
| **Encapsulation** (private setters) | `Movie`, `MovieCopy`, `Rental` | Καμία εξωτερική αυθαίρετη μετάλλαξη |
| **Encapsulated collections** | `Movie._copies` → `IReadOnlyCollection<MovieCopy> Copies` | Backing field· δεν εκτίθεται mutable list |
| **Factory methods** | `Movie.Create(...)`, `Rental.Start(...)` | Δημιουργία με εγγυημένα invariants |
| **State-transition methods** | `MovieCopy.Rent()/Return()`, `Rental.Return(now)`, `Rental.UpdateByEmployee(...)` | Οι αλλαγές κατάστασης περνούν από μεθόδους |
| **Invariant enforcement** | `Rent()` πετάει αν `!IsAvailable`· `Return()` πετάει αν ήδη επιστράφηκε | Το domain προστατεύει τον εαυτό του |
| **Computed properties** | `Movie.AvailableCopiesCount/TotalCopies`, `AppUser.FullName/ActiveRentalsCount`, `Rental.IsReturned/IsOverdue(now)/DaysLate(now)` | Παράγωγα δεδομένα, `[NotMapped]` |
| **Domain methods αντί queries** | `Movie.CanBeDeleted()` | Επιχειρηματικός κανόνας ως μέθοδος |
| **Clock-as-parameter** | `now` περνά ως όρισμα — **κανένα `DateTime.UtcNow` μέσα στα entities** | Καθαρό & testable domain |
| **Domain exceptions** | `Exceptions/DomainException` (base) + `CopyNotAvailableException`, `RentalAlreadyReturnedException` | Για παραβίαση invariant (όχι αναμενόμενα σφάλματα) |
| **Internal constructors / aggregate boundary** | `internal MovieCopy()` — copies δημιουργούνται μόνο μέσω `Movie` | Aggregate root ελέγχει τα μέλη του |

### Building blocks (Core/Common & Core/Configuration & Core/Queries)
| Concept | Τύπος | Σκοπός |
|---|---|---|
| **Result pattern** | `Result`, `Result<T>` | Αναμενόμενες αποτυχίες χωρίς exceptions |
| **Generic paged result** | `PagedResult<T>` (Items, Page, PageSize, TotalCount, TotalPages, HasNext/Previous) | Επαναχρησιμοποιήσιμο pagination payload |
| **Parameter Object** | `PagedQuery` + `MovieFilterQuery`/`CustomerFilterQuery`/`RentalFilterQuery` | Αντί για πολλά loose args (Clean Code F1) |
| **Options με defaults** | `RentalOptions` (το «7»), `PagingOptions` (10/5) | Named constants + configurable |
| **Enums** | `MovieGenre`, `RoleName` | `nameof(RoleName.Admin)` αντί magic strings |

---

## 3. Application / Service layer

| Concept | Πού | Λεπτομέρεια |
|---|---|---|
| **Service Layer pattern** | `IMovieService/ICustomerService/IRentalService` (Core) → impl (Infrastructure) | Orchestration, όχι λογική στους controllers |
| **Interfaces στο Core, impl στο Infrastructure** | `Core/Services` + `Infrastructure/Services` | Dependency inversion |
| **Result-returning commands** | `RentAsync`/`ReturnAsync`/`Create/Update/DeleteMovieAsync` → `Result`/`Result<int>` | Ξεκάθαρο success/failure |
| **Entity-returning queries** | `GetMoviesAsync` → `PagedResult<Movie>`, `GetRentalDetailsAsync` → `Rental?` | Controller κάνει το mapping |
| **Transaction boundary = single SaveChanges** | π.χ. `RentAsync` (copy.Rent + add rental σε ένα SaveChanges) | EF wrap-άρει σε implicit transaction· αφαιρέθηκαν τα χειροκίνητα `BeginTransaction` |
| **Concurrency handling** | `catch (DbUpdateConcurrencyException)` στο `RentAsync` → friendly `Result.Failure` | Optimistic concurrency UX |
| **Centralized query logic (DRY)** | `CustomerService.CustomersInRole()` (αντί 3× επανάληψη) | Single source of truth |
| **Options consumption** | `IOptions<RentalOptions>` injected στο `RentalService` | Το «7» έρχεται από config |
| **Reusable pagination** | `Infrastructure/Common/QueryableExtensions.ToPagedResultAsync()` | Count + clamp + Skip/Take σε ένα σημείο |

---

## 4. Persistence — EF Core 8 (`VideoClub.Infrastructure`)

| Concept | Πού | Λεπτομέρεια |
|---|---|---|
| **EF Core 8 + SQL Server** | `ApplicationDbContext`, `UseSqlServer` | LocalDB σε dev |
| **Code-first Migrations** | `Infrastructure/Migrations/*` | `dotnet ef migrations add` / `database update` |
| **DbContext = Unit of Work + Repository** | Τα services χρησιμοποιούν το `DbContext` απευθείας | **Όχι** generic repository (σκόπιμα — αποφυγή over-engineering) |
| **EF mapping rich domain** | private setters (το EF τα γράφει)· `private/internal` ctors· `PropertyAccessMode.Field` για encapsulated navs· `[NotMapped]` computed | Κρίσιμα gotchas |
| **Backing-field navigations** | `builder.Entity<Movie>().Navigation(m => m.Copies).UsePropertyAccessMode(PropertyAccessMode.Field)` | Το EF γράφει στο `_copies` |
| **Optimistic concurrency token** | `MovieCopy.RowVersion` (`rowversion`/`[Timestamp]` → fluent `IsRowVersion()`) | Προστασία race condition στο τελευταίο αντίτυπο |
| **Provider-aware model config** | `if (Database.IsSqlServer()) ... else Ignore(RowVersion)` | RowVersion μόνο σε SQL Server· επιτρέπει SQLite/InMemory στα tests |
| **Relationship config** | `HasOne/WithMany/HasForeignKey/OnDelete(DeleteBehavior.Restrict)` | Ρητά FK & delete behavior |
| **`AsNoTracking` σε reads** | Όλα τα query methods | Performance |
| **Filtered Include + relationship fixup** | `GetCustomerWithRentalsAsync` (`Include(u => u.Rentals.OrderByDescending(...))`) | Ordered child load· το EF γεμίζει το inverse `r.User` |
| **Transient resiliency** | `UseSqlServer(cs, sql => sql.EnableRetryOnFailure())` | Retry σε transient SQL σφάλματα |
| **Migrate-on-startup** | `DataSeeder` → `await db.Database.MigrateAsync()` | Auto-apply migrations πριν seeding |
| **Data seeding** | `DataSeeder.SeedAsync` (roles, admin, customers, movies μέσω `Movie.Create`) | Idempotent (ελέγχει ύπαρξη) |
| **Identity schema** | `IdentityDbContext<AppUser>` | AspNetUsers/Roles/UserRoles tables |

---

## 5. Web / Presentation — ASP.NET Core MVC (`VideoClub.Web`)

| Concept | Πού | Λεπτομέρεια |
|---|---|---|
| **MVC (Controllers + Razor Views)** | `Controllers/`, `Areas/Admin/`, `Views/` | |
| **Areas** | `Areas/Admin/Controllers` + `Areas/Admin/Views` + area route `{area:exists}/...` | Module routing `/Admin/Movies/...` |
| **Thin controllers** | bind → service → AutoMapper → μετάφραση `Result` σε `TempData` | **Μηδέν** `DbContext`/business logic |
| **DTOs / ViewModels** | `Web/DTOs/*` οργανωμένα σε φακέλους· `record` με `init` (immutable) | Data structures (Clean Code κεφ. 6) — σκόπιμα «anemic» |
| **AutoMapper** | `Mapping/MappingProfile`· `AssertConfigurationIsValid()` στο startup | Entity → DTO· computed props καθάρισαν χειροκίνητα maps |
| **Model binding parameter objects** | actions δέχονται filter/query args | |
| **Validation** | DataAnnotations (`[Required]`, `[StringLength]`, `[Range]`) + `ModelState.IsValid` | Server-side validation |
| **PRG pattern** | POST → `RedirectToAction` + `TempData["Message"]/["Error"]` | Αποφυγή double-submit |
| **Tag helpers & routing** | `asp-action/asp-controller/asp-area/asp-route-*` | Cross-area/controller links |
| **Authentication & Authorization** | ASP.NET Core Identity· `[Authorize(Roles = nameof(RoleName.Admin))]` | Role-based, ανά controller |
| **Anti-forgery (global)** | `AutoValidateAntiforgeryTokenAttribute` ως global filter | CSRF protection |
| **Global exception middleware** | `Web/Middleware/ExceptionHandlingMiddleware` (log `DomainException`=Warn, άλλα=Error → `/Home/Error`) | Production error handling |
| **Serilog request logging** | `app.UseSerilogRequestLogging()` | Structured HTTP logs |
| **JSON endpoint + AJAX** | `RentalsController.CheckOverdue` → `Json(...)`· `site.js` data-check-url | Overdue confirmation modal |
| **Razor infrastructure** | `_Layout`, `_ViewStart`, `_ViewImports`, partials (`_LoginPartial`, `_ValidationScriptsPartial`)· area-specific `_ViewImports/_ViewStart` | View location fallback `/Views/Shared` |

---

## 6. Cross-cutting concerns

| Concept | Υλοποίηση |
|---|---|
| **Configuration / IOptions pattern** | `appsettings.json` sections `Rental`/`Paging` → `Configure<T>()` → `IOptions<T>` injected |
| **Structured logging** | Serilog (Console + rolling File sink), levels, `Logs/` ignored στο git |
| **Error-handling strategy** | `Result` για αναμενόμενα· `DomainException` για invariants· middleware ως backstop (Clean Code κεφ. 7) |
| **Optimistic concurrency** | `RowVersion` + `DbUpdateConcurrencyException` |
| **Encoding hygiene** | UTF-8 source files (διορθώθηκε mojibake bug) |

---

## 7. Testing (`VideoClub.Tests`, xUnit) — 27 tests

| Concept | Πού | Λεπτομέρεια |
|---|---|---|
| **xUnit** | `[Fact]`, `[Theory]/[InlineData]` | |
| **Pure domain unit tests** | `Domain/MovieTests`, `MovieCopyTests`, `RentalTests` | Χωρίς DB — εδώ «πληρώνεται» το rich domain (γρήγορα, ντετερμινιστικά) |
| **Service/integration tests με SQLite in-memory** | `Services/SqliteTestBase` (ανοιχτή `SqliteConnection` + `EnsureCreated`) | Επιβάλλει σχεσιακούς κανόνες/FK (σε αντίθεση με τον EF InMemory provider) |
| **Test doubles χωρίς framework** | `NullLogger<T>.Instance`, `Options.Create(new RentalOptions())` | Χωρίς mocking lib |
| **Provider-aware model** | επιτρέπει SQLite στα tests ενώ κρατά SQL Server σε prod | |
| **Test isolation** | νέα in-memory βάση ανά test (xUnit instance-per-test) | |

---

## 8. Tooling / Build / Language

- **.NET 8 / C# 12:** file-scoped namespaces, `record` types, `init` setters, collection expressions `[]`, target-typed `new`, expression-bodied members.
- **Nullable reference types** enabled (`<Nullable>enable</Nullable>`).
- **ImplicitUsings** enabled.
- **Solution format:** `.slnx` (XML-based).
- **CLI:** `dotnet build` / `dotnet test` / `dotnet ef`.
- **Verification ως gate:** build (τα Razor views compile-ονται → πιάνει view/model errors) + `dotnet test`.

---

## 9. Clean Code principles που εφαρμόστηκαν

| Αρχή | Πώς |
|---|---|
| **SRP** | Controllers thin· services ανά aggregate· entities κρατούν τη δική τους λογική |
| **DRY** (G5) | Ενοποίηση: 2 rent flows → `RentAsync`· pagination → `ToPagedResultAsync`· role query → `CustomersInRole` |
| **No magic numbers** (G25) | `7` → `RentalOptions.RentalPeriodDays`· page sizes → `PagingOptions` |
| **Few arguments** (F1) | Parameter objects (`PagedQuery` + filters) |
| **Objects vs Data Structures** (κεφ. 6) | Entities = objects (συμπεριφορά)· DTOs = data structures (μόνο δεδομένα) |
| **Command-Query Separation** | Queries επιστρέφουν δεδομένα· commands επιστρέφουν `Result` |
| **Error handling** (κεφ. 7) | Define normal flow με `Result`· exceptions μόνο για το πραγματικά εξαιρετικό |
| **Meaningful names** | `RentFromMovie`, `AvailableCopiesCount`, `CanBeDeleted` κ.λπ. |

---

## 10. Mapping σε Umbraco 17 LTS / B2B Loyalty Platform

Πώς μεταφέρεται κάθε concept στο target σου. **Επιβεβαίωσε version-specific APIs με τα Umbraco 17 docs** (το backoffice από v14+ είναι API-driven).

### Τι μένει σχεδόν ίδιο (καθαρά .NET — μεταφέρεται 1:1)
- **Layering / Clean Architecture:** κράτα `Loyalty.Core` (domain) + `Loyalty.Application` (ή services στο Core) + `Loyalty.Infrastructure` (EF Core) **καθαρά από Umbraco**. Το Umbraco web project είναι ο host/composition root που τα αναφέρει.
- **Rich domain model, factories, invariants, value/computed props, domain exceptions, clock-as-parameter** → ακριβώς τα ίδια (π.χ. `LoyaltyAccount`, `PointsTransaction`, `Reward`, `Tier` με `Earn()/Redeem()` και invariants όπως «δεν εξαργυρώνεις πάνω από το balance»).
- **Result pattern, PagedResult/PagedQuery, parameter objects, Options pattern** → όλα μεταφέρονται αυτούσια.
- **Service layer (interfaces στο Core, impl στο Infrastructure)** → ίδιο pattern.
- **EF Core 8/9** για τα **custom loyalty tables** → ίδια migrations, encapsulated collections, concurrency tokens, AsNoTracking. (Σημαντικό: το loyalty domain ΔΕΝ είναι Umbraco content — είναι δικά σου tables.)
- **AutoMapper, Serilog, xUnit, SQLite tests** → ίδια (Serilog είναι ήδη ενσωματωμένο στο Umbraco).
- **IOptions / appsettings** → ίδιο.

### Τι αλλάζει στο Umbraco (αντιστοιχίες)
| VideoClub (ASP.NET Core MVC) | Umbraco 17 αντίστοιχο |
|---|---|
| `Program.cs` DI wiring / `AddInfrastructure()` | **Composer** (`IComposer.Compose(builder)`) ή `builder.Services` στο `Program.cs`· `builder.AddUmbraco()` |
| `ApplicationDbContext` (custom DI) | Δικό σου EF Core `DbContext` (custom tables) registered παράλληλα με το Umbraco — Umbraco έχει και δικό του `IScopeProvider`/Management API· για custom data συνήθως δικός σου EF Core context + `MigrationPlan`/`IMigration` ή EF migrations |
| ASP.NET Core **Identity** (`AppUser`) | **Umbraco Members** για front-end B2B χρήστες (`IMemberService`/`IMemberManager`)· **Backoffice Users** ξεχωριστά (διαχειριστές) |
| MVC **Controllers / Areas** | **Render controllers** (route hijacking), **Surface controllers** (form POSTs σε views), **(Backoffice) Management API controllers** (v14+ νέο backoffice), ή plain **API controllers** για B2B endpoints |
| Razor Views / Tag helpers / `_Layout` | Ίδια Razor — αλλά τα templates/views συνδέονται με **Document Types**· για headless/SPA backoffice χρησιμοποιείς το Management API |
| `[Authorize(Roles=...)]` | Member groups / `[Authorize]` με member auth policies· backoffice authorization για admin tooling |
| `DataSeeder` + `MigrateAsync` στο startup | **`INotificationHandler<UmbracoApplicationStartingNotification>`** (ή migration component) για seeding/migrations· Umbraco τρέχει τα δικά του migrations αυτόματα |
| AutoMapper profiles | AutoMapper (όπως τώρα) **ή** ο ενσωματωμένος **`IUmbracoMapper`** για content→viewmodel |
| Global exception middleware | Ίδιο middleware στο pipeline (`app.UseMiddleware<...>()` μετά το `u.UseBackOffice()`/`u.UseWebsite()`) |
| Anti-forgery global filter | Ίδιο — αλλά πρόσεχε τη συνύπαρξη με Umbraco surface controllers (έχουν δικό τους anti-forgery handling) |

### Πρακτικές συστάσεις για το loyalty platform
1. **Κράτα το domain εκτός Umbraco.** Το Umbraco να είναι μόνο ο host + presentation/integration layer. Τα `Loyalty.Core/Application/Infrastructure` δεν πρέπει να αναφέρουν Umbraco packages.
2. **Custom tables, όχι content,** για points/transactions/tiers/rewards (υψηλός όγκος, σχεσιακά queries, concurrency) → δικό σου EF Core context + migrations. Το Umbraco content μένει για marketing/CMS σελίδες, όχι transactional data.
3. **Members = B2B accounts/contacts** (front-end auth). Σκέψου B2B ιεραρχία: company ↔ users (member groups ή custom relation).
4. **Concurrency token** στα balance/points tables — εδώ είναι **πιο κρίσιμο** από το VideoClub (ταυτόχρονες εξαργυρώσεις).
5. **Result pattern + domain exceptions** για κανόνες loyalty (insufficient points, expired tier, reward out of stock).
6. **Notifications/Composers** για cross-cutting (π.χ. όταν δημιουργείται member → δημιουργία `LoyaltyAccount`).
7. **Tests:** pure domain tests για τους κανόνες πόντων/tiers (μεγάλη αξία)· SQLite integration tests για τα services.

---

## TL;DR checklist (concepts να μεταφερθούν)

- [ ] Layered solution + dependency inversion (interfaces στο Core)
- [ ] Rich domain entities (encapsulation, factories, invariants, state transitions, computed props)
- [ ] Domain exceptions + `Result`/`Result<T>` (invariants vs αναμενόμενα σφάλματα)
- [ ] Service layer (orchestration, single-SaveChanges transactions, concurrency handling)
- [ ] Parameter objects (`PagedQuery`+filters) + generic `PagedResult<T>` + pagination extension
- [ ] Options pattern (named constants → `IOptions` από appsettings)
- [ ] EF Core: migrations, encapsulated-collection mapping, concurrency token, provider-aware config, AsNoTracking, filtered includes, retry-on-failure
- [ ] Thin presentation (controllers/API), DTOs/viewmodels, AutoMapper, validation, PRG
- [ ] AuthN/AuthZ (Members + backoffice), anti-forgery
- [ ] Global exception middleware + Serilog (structured) request logging
- [ ] xUnit: pure domain tests + SQLite-backed service tests
- [ ] Clean Code: SRP, DRY, no magic numbers, few args, objects-vs-data-structures, CQS, error handling
