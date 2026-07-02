# KioskRewards — Project Context & Session Handoff

> **Για νέα session:** Διάβασε ΟΛΟ αυτό το αρχείο πρώτα, μετά συνέχισε από το **Phase 7** (§9).
> Το project είναι ένα **practice project** για εξοικείωση με **Umbraco 17 LTS** πάνω σε **Clean Architecture**.
> Working language: **Ελληνικά** (τεχνικοί όροι/identifiers στα Αγγλικά). Ρυθμός: **φάση-φάση** — εξήγησε → γράψε → τρέξε/verify → επιβεβαίωσε → προχώρα. Ο χρήστης θέλει **έξτρα concept explanations** γιατί πρώτη φορά δουλεύει Umbraco (αλλά είναι έμπειρος σε .NET/Clean Architecture).

---

## 1. Στόχος / Σενάριο

Mini **B2B loyalty platform**: μια εταιρεία επιβραβεύει περιπτεράδες που πουλάνε τα προϊόντα της. Ο περιπτεράς κάνει login, βλέπει **υπόλοιπο πόντων** + **ιστορικό συναλλαγών**, και **εξαργυρώνει** πόντους σε δώρα από κατάλογο. Δίγλωσσο (EL/EN — προσομοιώνει «3 χώρες»). Το Umbraco είναι ο **host + CMS + presentation**· η loyalty λογική είναι **καθαρό .NET** από κάτω.

**Έμπνευση:** προηγούμενο project του χρήστη στο `_reference/VideoClubV2/` (rich domain + service layer, EF Core, xUnit). Ο μπούσουλας concepts: `_reference/VideoClubV2/docs/IMPLEMENTED-CONCEPTS.md`.

---

## 2. Stack & Solution structure

- **.NET 10 SDK**, **Umbraco 17.x LTS** (pinned `17.*`), **EF Core 10.0.6** (pinned για να ταιριάζει με του Umbraco — αλλιώς version conflict στο Web process), **SQLite** (dev), **xUnit**.
- ModelsBuilder = **SourceCodeAuto** (τα Document Types γίνονται C# κλάσεις σε build). Delivery API on.
- Solution: classic `.sln` (για VS 2022), 5 projects:

```
KioskRewards.sln
├── KioskRewards.Web            ← Umbraco host / presentation (Composers, Views, Program.cs)
├── KioskRewards.Application    ← abstractions & DTOs (IPointsService, PointsTransactionDto)  — μηδέν EF
├── KioskRewards.Domain         ← entities/rules (PointsAccount, PointsTransaction, Result, exceptions)
├── KioskRewards.Infrastructure ← EF Core (LoyaltyDbContext, PointsService, seeder, migrations, DI)
└── KioskRewards.Tests          ← xUnit (domain + SQLite integration) — 20 tests, all green
```
References: `Web → Application + Infrastructure`, `Infrastructure → Application`, `Application → Domain`, `Tests → Domain + Application + Infrastructure`.

> `KioskRewards.Web` είναι το **startup project** (το `.sln` το έχει πρώτο· ζωντανά στο `.vs/.../.suo`). `.gitignore` υπάρχει (αγνοεί `_reference/`, build artifacts, SQLite db). Το KioskRewards **δεν** είναι δικό του git repo (κάθεται μέσα στο `C:\Users\seyme`).

---

## 3. Πρόοδος ανά Phase

- **Phase 0 — Setup** ✅ (Clean Architecture solution, Umbraco 17 boot-verified)
- **Phase 1 — Domain + Application + Infrastructure core (+tests)** ✅ (domain-first· 20 tests green)
- **Phase 1.5 — Wire core into Umbraco** ✅ (Composer DI + startup seeding· verified με seeded data)
- **Phase 2 — Content Foundation** ✅ (Document Types + templates + marketing σελίδες· verified render)
- **Phase 3 — Members & Authentication** ✅ (Member Type/Group + login/logout + Public Access + auto-provision loyalty account· verified end-to-end)
- **Phase 4 — Member Dashboard** ✅ (route hijacking· πραγματικό balance + history από IPointsService· verified στο browser)
- **Phase 5 — Rewards Catalog & Redemption** ✅ (Reward = Umbraco content, convention-rendered catalog + Surface Controller redeem μέσω του υπάρχοντος `IPointsService`· verified end-to-end στο browser)
- **Phase 6 — Πολυγλωσσικότητα (EL/EN)** ✅ (languages + path-based routing + variant content + Dictionary items + πραγματικό language switcher· verified end-to-end και στις δύο γλώσσες — βλ. §8 για detail)
- **Phase 7 — Stretch (Examine search / JSON API / uSync)** ⏭️ **ΕΠΟΜΕΝΟ** (βλ. §9 — ΠΡΟΣΧΕΔΙΟ, να διαλέξουμε κατεύθυνση πριν ξεκινήσουμε)
- Phase 8 — Review

Roadmap = **domain-first**: χτίσαμε τον testable πυρήνα πρώτα (comfort zone), μετά βάζουμε Umbraco από πάνω.

---

## 4. Architecture & key design decisions

- **Οι «χρήστες» (περιπτεράδες) ΕΙΝΑΙ Umbraco Members — όχι domain entity.** Το loyalty domain τους αναφέρει μόνο μέσω **`MemberKey` (Guid)** = το `Member.Key` του Umbraco. Το `PointsAccount` είναι ο περιπτεράς όπως τον βλέπει το loyalty bounded-context (1 account ανά member).
- **Rich domain (όχι anemic):** `PointsAccount` aggregate root — PK = `MemberKey`, materialized `Balance`, `RowVersion` concurrency token, encapsulated `_transactions` (backing field). Factory `Create`, μέθοδοι `Earn/Redeem/CanRedeem`, invariant «δεν πας κάτω από 0». `PointsTransaction` = immutable ledger entry (internal ctor → φτιάχνεται μόνο μέσω του account). Clock-as-parameter (`nowUtc` περνά απ' έξω).
- **Exception vs Result:** ο service καλεί `CanRedeem` πρώτα → `Result.Failure` για αναμενόμενο «λίγοι πόντοι»· το `InsufficientPointsException` του domain είναι backstop.
- **Καμία `ILoyaltyRepository`** — το `LoyaltyDbContext` είναι repository + unit of work (όπως VideoClub). Abstraction = `IPointsService` (Application), impl = `PointsService` (Infrastructure).
- **Provider-aware concurrency:** RowVersion ignored σε SQLite, `IsRowVersion()` σε SQL Server (γίνεται στο `LoyaltyDbContext.OnModelCreating` με `Database.IsSqlite()`).
- **Composition → C# interface:** το SEO composition έγινε interface `ISeo`· `HomePage`/`ContentPage : ..., ISeo`.
- **Members ↔ loyalty seed — αποφασίστηκε (β):** `MemberSavedLoyaltyHandler` (`INotificationAsyncHandler<MemberSavedNotification>`) δημιουργεί αυτόματα `PointsAccount` + 100 welcome points στο πρώτο save ενός `kioskOwner` member. Idempotent μέσω `GetHistoryAsync` (αν έχει ήδη ιστορικό, δεν ξαναδίνει bonus) — όχι flag στο domain, το invariant ζει στο handler. Το παλιό fixed-Guid demo seed (`LoyaltyDataSeeder`) μένει ως έχει, ανεξάρτητο.
- **Reward = Umbraco content, ΟΧΙ domain entity/EF table (Phase 5, εκτελέστηκε).** Document Types `Reward` (Title, Description, PointsCost, Image media-picker) + `RewardsCatalog` (container, allowed child = `Reward`, καμία δική του property). Η εξαργύρωση περνάει αναλλοίωτη από το ήδη υπάρχον `IPointsService.RedeemAsync` — μηδέν αλλαγές στο domain/EF core.
- **Convention rendering + `@inject` αντί για route hijacking, όταν δεν χρειάζεται να «κόψεις» το render.** Το `RewardsCatalog.cshtml` δεν έχει δικό του controller· κάνει `@inject IMemberManager` + `@inject IPointsService` απευθείας στο view (ίδιο idiom με το `Partials/_LoginStatus.cshtml`) για να διαβάσει login state + balance πριν δείξει τα κουμπιά Redeem. Route hijacking (`RenderController` subclass) κρατιέται μόνο για σελίδες που πραγματικά χρειάζονται λογική πριν αποφασίσουν *τι* θα δείξουν (π.χ. `MemberDashboard`), όχι για απλά read-only lookups.
- **Πολυγλωσσικότητα (Phase 6, εκτελέστηκε) — variant content vs Dictionary items, όχι μπερδεμένα μεταξύ τους:** πραγματικό μεταφράσιμο περιεχόμενο (Heading/BodyText σε HomePage/ContentPage, Title/Description σε Reward) → **variant properties** (Vary by culture, backoffice-only, ο editor το φτιάχνει)· static UI chrome (labels, buttons, μηνύματα σε views/controllers) → **Dictionary items** (Settings→Translation) διαβασμένα μέσω `UmbracoHelper.GetDictionaryValue()` / `ICultureDictionaryFactory`. Το **loyalty domain/EF core παραμένει 100% culture-agnostic** — το `IPointsService`/`PointsAccount` δεν άλλαξε καθόλου· ακόμα και το `result.Error` message από το `RedeemAsync` μένει σκόπιμα English (domain-layer text, όχι presentation).
- **Routing ανά γλώσσα: path-based, χωρίς πραγματικά hostnames.** Culture and Hostnames στο root (`Home`) node μόνο: Culture = Greek (default, χωρίς prefix), Hostnames = ένα entry με σχετικό path `/en` → English. Παιδιά = "Inherit" (καμία ανάγκη per-node config).

---

## 5. Umbraco concepts (μαθημένα — για συνέχεια του teaching)

- **Settings vs Content:** Document Type = «class/schema» (το ορίζεις μία φορά στο Settings)· Content node = «object/row» (φτιάχνεις πολλά στο Content). Composition = interface/base. Αυτός ο διαχωρισμός επιτρέπει σε μη-developer να βάζει σελίδες χωρίς κώδικα.
- **Χρυσός κανόνας:** σχήμα + περιεχόμενο → backoffice· συμπεριφορά + λογική → κώδικας.
- **Composer = το `Program.cs` DI wiring σε Umbraco μορφή** (`IComposer`, auto-discovered μέσω `.AddComposers()` — δεν αγγίξαμε το Program.cs).
- **Startup work = notification handler** (`INotificationAsyncHandler<UmbracoApplicationStartedNotification>`).
- **Convention-based rendering:** Umbraco matchάρει published node → Template (by alias) → `Views/HomePage.cshtml`. Καμία ανάγκη controller. (Route hijacking — δες παρακάτω — χρειάζεται μόνο όταν θες λογική πριν το render.)
- **Strongly-typed views:** `@inherits UmbracoViewPage<HomePage>` → `@Model.Heading` με IntelliSense (κλάσεις από ModelsBuilder).
- **Δυναμικό nav:** `Model.AncestorOrSelf(1).Children()` → menu από το content tree.
- **Umbraco namespaces:** `IComposer`→`Core.Composing` · `IUmbracoBuilder`+`AddNotificationAsyncHandler`→`Core.DependencyInjection` · notifications→`Core.Notifications` · `INotificationAsyncHandler`→`Core.Events` · `IRuntimeState`→`Core.Services` · `RuntimeLevel`→`Core` · `UmbracoHelper`→`Web.Common` (όχι `Web.Common.Views`) · `ICultureDictionaryFactory`/`ICultureDictionary`→`Core.Dictionary`.
- **Members ≠ backoffice Users:** δύο εντελώς ξεχωριστά auth συστήματα/tables (`umbracoMember*` vs `umbracoUser*`), ξεχωριστό login cookie. Backoffice admin credentials δεν δουλεύουν ποτέ σε member login.
- **Surface Controller = form POST μέσα σε published σελίδα** (`: SurfaceController`, `Html.BeginUmbracoForm<T>(action)` στο view). Auto-παίρνει antiforgery token + Umbraco route token (`ufprt`, επιβάλλεται με `[ValidateUmbracoFormRouteString]`). `CurrentUmbracoPage()` = re-render του ίδιου node με ModelState errors (το PRG equivalent για validation failures)· `RedirectToCurrentUmbracoPage()` = redirect μετά από success.
- **Member auth API:** `IMemberSignInManager.PasswordSignInAsync(username, password, isPersistent, lockoutOnFailure)` → `SignInResult` (Succeeded/IsLockedOut/IsNotAllowed/RequiresTwoFactor). `IMemberManager.IsLoggedIn()` (sync bool) + `GetCurrentMemberAsync()` (Task, null αν ανώνυμος).
- **Public Access δεν κάνει HTTP redirect.** Κάνει **internal execute** της login-page template κρατώντας το αρχικό URL στην address bar (status 200, όχι 302). Σωστό behavior — όχι bug (μπέρδεψε αρχικά όταν φαινόταν σαν να μη δούλευε το restriction).
- **`MemberSavedNotification`** πυροδοτείται σε ΚΑΘΕ save member (create ΚΑΙ edit) — handlers πρέπει να είναι idempotent.
- **Route hijacking = naming convention `[DocumentTypeAlias]Controller`.** `MemberDashboardController : RenderController` για alias `memberDashboard` — καμία ρητή registration, το matching γίνεται by class name. Base ctor χρειάζεται `(ILogger<T>, ICompositeViewEngine, IUmbracoContextAccessor)`. Αν το action name ταιριάζει με το template name, εκτελείται αντί για `Index()`.
- **Custom ViewModel πάνω σε generated model — αυστηρό constructor shape.** Αν κάνεις `MyViewModel : GeneratedModel`, το `[PublishedModel]` attribute **κληρονομείται**, και η `PublishedModelFactory` απαιτεί το ΙΔΙΟ `(IPublishedContent, IPublishedValueFallback)` constructor σε κάθε subclass (σαρώνει όλα). Extra δεδομένα → **settable/`init` properties + object initializer** στον controller, ΟΧΙ extra ctor params (αλλιώς boot crash: «missing a public constructor with one argument... IPublishedElement»).
- **Media Picker property → `MediaWithCrops`, με `.Content` (όχι `.MediaItem`).** Το ModelsBuilder-generated property τύπου Media Picker επιστρέφει `Umbraco.Cms.Core.Models.MediaWithCrops`· το πραγματικό `IPublishedContent` του media είναι στο `.Content` (`reward.Image?.Content?.Url()`), όχι σε κάποιο `.MediaItem` (δεν υπάρχει τέτοιο member — compile error αν το υποθέσεις).
- **Νέο Document Type στο backoffice ΔΕΝ δημιουργεί αυτόματα Template.** Διαφορετικό από το gotcha #7 (εκεί το template υπάρχει αλλά είναι άδειο) — εδώ γενικά δεν υπάρχει καθόλου σύνδεση. Χρειάζεται: (1) Settings → Templates folder → δημιούργησε νέο Template με το ΙΔΙΟ PascalCase όνομα με το `.cshtml` που θα γράψει το AI· (2) πίσω στο Document Type → tab **Templates** (ξεχωριστό tab, όχι Design/Structure/Settings) → Choose → επίλεξε/όρισε default· (3) αν το content node είχε δημιουργηθεί *πριν* υπάρξει το Template, το δικό του πεδίο **Template** (στο Info panel) μπορεί να μείνει κενό ακόμα και μετά re-publish — έλεγξέ το/όρισέ το ρητά εκεί αν επιμένει το 404.
- **Variant vs Invariant properties — δύο-επίπεδο toggle.** (1) Document Type-level "Allow vary by culture" (tab **Settings** → Data variations) = πύλη, επιτρέπει σε properties να ποικίλλουν. (2) Κάθε property ξεχωριστά έχει *δικό του* toggle "Shared across cultures" (Design tab → click property → Variation section) που **default μένει ON (invariant)** ακόμα κι όταν το (1) είναι ενεργό — πρέπει να το σβήσεις ρητά per-property για να πάρεις πραγματικά ξεχωριστές τιμές ανά culture. Ξεχνώντας το (2) → όλα φαίνονται να δουλεύουν στο UI αλλά το περιεχόμενο μένει ίδιο και στα δύο cultures (βλ. gotcha #14).
- **Node `Name` είναι ΠΑΝΤΑ culture-variant μόλις υπάρξουν 2+ γλώσσες στο site** — ανεξάρτητα αν το ίδιο το Document Type έχει έστω ένα variant property. Άρα το URL slug μιας σελίδας είναι ό,τι Name έχει *αυτό* το culture (π.χ. Greek Name "Σύνδεση" → `/σύνδεση/`), όχι απαραίτητα κάτι προβλέψιμο σαν το αγγλικό λεκτικό.
- **System "Default language" (Settings→Languages) ≠ node "Culture" (Culture and Hostnames).** Το install δημιουργεί πάντα πρώτα English (United States) ως Default — αόρατο όσο το περιεχόμενο είναι invariant. Για να γίνει μια άλλη γλώσσα το «no-prefix root», χρειάζεται να αλλάξεις **και τα δύο**: το system Default flag (Settings→Languages→click language→toggle "Default language") **και** το Culture του root content node.
- **Culture and Hostnames: relative path αντί για πραγματικό domain.** Στο Hostnames section, μπορείς να βάλεις μια τιμή σαν `/en` (όχι πραγματικό hostname) — το Umbraco την αναγνωρίζει σαν path-prefix. Μόνο το root node χρειάζεται ρητή ρύθμιση· τα παιδιά μένουν "Inherit".
- **Dictionary items (Settings→Translation) vs `ILocalizedTextService`: δύο ξεχωριστά συστήματα.** Editor-managed μεταφράσεις static UI strings → Dictionary items, διαβάζονται με `Umbraco.GetDictionaryValue("Key")` (views, μέσω `UmbracoHelper`) ή `ICultureDictionaryFactory.CreateDictionary()["Key"]` (controllers, namespace `Umbraco.Cms.Core.Dictionary`). Το `ILocalizedTextService` είναι *διαφορετικό*, χαμηλότερου επιπέδου σύστημα δεμένο σε XML language files (`~/config/lang/*.xml`) για backoffice/system text — ΔΕΝ είναι αυτό που θες για editor-managed content translations.
- **Umbraco θέτει ambient `CultureInfo.CurrentUICulture` per-request** ώστε να ταιριάζει με το resolved culture του URL — γι' αυτό `GetDictionaryValue()`/`CreateDictionary()` «απλά δουλεύουν» χωρίς να περάσεις culture ρητά.
- **Πραγματικό language switcher:** `IPublishedContent.Cultures.Keys` (ποια cultures έχει published το *τρέχον* node) + `.Url(culture: "en-US")` (το URL του *ίδιου* node σε άλλο culture). Δουλεύει σε κάθε σελίδα επειδή το `Model` του `_Layout.cshtml` είναι πάντα το τρέχον-rendered content, όχι μόνο η αρχική.

---

## 6. GOTCHAS (πραγματικά bugs που λύσαμε — μην ξαναπατηθούν)

1. **`|DataDirectory|` token:** ο `Microsoft.Data.Sqlite` ΔΕΝ το αναλύει (μόνο το Umbraco, για το δικό του DB). Για το δικό μας EF context το αναλύουμε εμείς: `AddLoyaltyInfrastructure` δέχεται `Func<IServiceProvider,string>`· ο Composer βρίσκει `IHostEnvironment.ContentRootPath` + `umbraco/Data` σε runtime και κάνει `.Replace`.
2. **Razor `@page`:** μην ονομάζεις loop μεταβλητή `page` — το `@page` διαβάζεται ως directive και σπάει όλο το view (incl. `@inherits`). (Το λύσαμε με `child`.)
3. **Rebuild rule:** το Umbraco compile-άρει τα `.cshtml` **στο build** (όχι runtime compilation). Άλλαξες template/DocType στον δίσκο → **Rebuild (Ctrl+Shift+B) + refresh**. Σύμπτωμα αν το ξεχάσεις: **HTTP 200 με άδειο body**. (Αλλαγές **περιεχομένου** στο Content ΔΕΝ θέλουν rebuild.)
4. **EF migrations χωρίς Umbraco:** υπάρχει `LoyaltyDbContextFactory : IDesignTimeDbContextFactory` ώστε `dotnet ef` να τρέχει χωρίς boot.
5. **Self-diagnosis εργαλεία:** browser View Source (Ctrl+U), DevTools→Network status, **Umbraco Log Viewer** (Settings→Advanced), VS Output window.
6. **Warning NU1903** (SQLitePCLRaw 2.1.11) = transitive, ίδιο package που φέρνει το Umbraco — αβλαβές, 0 errors.
7. **Νέο Document Type/Member Type → template scaffold είναι άδειο** (`Layout = null`, τίποτα άλλο). Σύμπτωμα: 200 OK αλλά «Cannot render the page» ή λευκή σελίδα. Πάντα γράψε το view μετά.
8. **Member login username ≠ backoffice admin.** Αν το login αποτυγχάνει με «Invalid username or password», πρώτος έλεγχος: μήπως δοκιμάστηκαν τα backoffice admin creds; Members section → open member → πεδίο **Username** (όχι απαραίτητα το email) + reset password εκεί.
9. **`RenderController.Index()` είναι sync-only** (γνωστός Umbraco περιορισμός, όχι δικό μας bug). Route-hijacking controllers που χρειάζονται async services (π.χ. `IMemberManager`, `IPointsService`) γράφουν το `Index()` ως λεπτό sync wrapper πάνω από ένα private `async Task<IActionResult> IndexAsync()`, με `.GetAwaiter().GetResult()`. Ασφαλές στο ASP.NET Core (δεν έχει `SynchronizationContext` → no deadlock).
10. **Custom ViewModel πάνω σε generated Umbraco model → boot crash αν προσθέσεις ctor params.** Βλ. §5 «Custom ViewModel — αυστηρό constructor shape». Fix: settable properties + object initializer.
11. **Media Picker property → `.Content`, ΟΧΙ `.MediaItem`.** Βλ. §5 «Media Picker property». Compile error αν μαντέψεις `.MediaItem`.
12. **Νέο Document Type ΔΕΝ παίρνει Template αυτόματα (Umbraco 17 backoffice).** Βλ. §5 «Νέο Document Type ΔΕΝ δημιουργεί αυτόματα Template». Σύμπτωμα: **404 "No template exists to render the document at URL..."** (διαφορετικό μήνυμα/αιτία από το #7). Fix σε 3 βήματα: δημιούργησε Template χειροκίνητα → assign το στο Document Type (tab **Templates**) → αν το content node προϋπήρχε, όρισε ρητά το δικό του πεδίο Template στο Info panel.
13. **Ενεργοποίηση 2ης γλώσσας σε ήδη-invariant site → χρειάζεται ρητό re-publish per node.** Το publish state γίνεται πλέον per-culture tracked, ακόμα και για nodes με invariant properties· τα υπάρχοντα nodes δεν καλύπτονται αυτόματα. Σύμπτωμα: σελίδες που δούλευαν πριν εξαφανίζονται από dynamic nav ή δίνουν 404. Fix: άνοιξε κάθε node → **Save and publish** με το νέο culture τσεκαρισμένο. Εύκολο να ξεχαστούν nodes εκτός του ορατού nav loop (π.χ. Login, Dashboard).
14. **Property vary-by-culture toggle σε επίπεδο Document Type δεν αρκεί.** Βλ. §5 «Variant vs Invariant properties — δύο-επίπεδο toggle». Σύμπτωμα: αλλάζεις το ένα culture tab στο content editor, αλλάζει και το άλλο (ίδια underlying τιμή) — χρειάζεται ΚΑΙ το per-property "Shared across cultures" OFF.
15. **Μην μαντεύεις culture-specific URLs.** Βλ. §5 «Node Name είναι πάντα culture-variant». Σύμπτωμα: `/login/` δίνει 404 ενώ η σελίδα υπάρχει και δουλεύει — το πραγματικό URL της είναι ό,τι λέει το Greek `Name` (π.χ. `/σύνδεση/`). Check: content editor → άλλαξε culture tab → δες το **Links** section για το πραγματικό URL εκείνου του culture.
16. **System "Default language" flag μπορεί να μην ταιριάζει με την «προφανή» πρωτεύουσα γλώσσα του project.** Βλ. §5. Το πρώτο-created language (συνήθως en-US από το install) κρατάει το Default flag αόρατα μέχρι να μπει 2η γλώσσα και να φανεί η ασυμφωνία. Fix: Settings→Languages→click στη γλώσσα που θες πρωτεύουσα→toggle "Default language" ON (αυτόματα σβήνει από την άλλη) — ΜΕΤΑ ευθυγράμμισε και το root node's Culture and Hostnames.
17. **Hardcoded relative-path strings σπάνε σιωπηλά κάτω από multi-culture routing.** `href="/dashboard/"`, `returnUrl = "/dashboard/"` κ.λπ. (γραμμένα πριν υπάρξει Phase 6) χάνουν το culture prefix (π.χ. πάνε σε `/dashboard/` αντί για `/en/dashboard/`). Fix: resolve το πραγματικό `IPublishedContent` node (`Model.AncestorOrSelf(1).DescendantsOrSelf<T>().FirstOrDefault()`) και κάλεσε `.Url()` (culture-aware μέσω ambient culture) αντί για literal string. Βρέθηκε/διορθώθηκε σε `_LoginStatus.cshtml`, `RewardsCatalog.cshtml`, `LoginPage.cshtml`.

---

## 7. How to run / dev info

- VS 2022 → άνοιξε `KioskRewards.sln` → **F5** (startup = KioskRewards.Web, IIS Express).
- URL: `https://localhost:44315` · Backoffice: `https://localhost:44315/umbraco`
- **Backoffice admin (unattended dev):** `admin@kioskrewards.local` / `ChangeMe12345!`  *(plain-text dev creds, ποτέ production)*
- DBs (SQLite, στο `KioskRewards.Web/umbraco/Data/`, gitignored): `Umbraco.sqlite.db` (CMS) + `loyalty.sqlite.db` (δικό μας).
- Connection string `LoyaltyDb` στο `appsettings.Development.json`.
- Tests: `dotnet test` (ή VS Test Explorer) → **20 green**.
- Create EF migration: `dotnet ef migrations add <Name> -p KioskRewards.Infrastructure -s KioskRewards.Infrastructure -o Persistence/Migrations`.

> **Σημ. για το AI σε νέα session:** όταν τρέχεις τον server από CLI για verify, ο IIS Express του VS κλειδώνει τα DLLs — σταμάτα τον πρώτα (`Get-Process iisexpress | Stop-Process -Force`) και καθάρισε port 44315 πριν το build/run. Μετά το verify, σκότωσε τον CLI server (match `dotnet.exe`/`KioskRewards.Web.exe` με cmdline `KioskRewards.Web`, ή port-owner). Στο Phase 6 το build verify (χωρίς full run) ήταν αρκετό κάθε φορά — ο χρήστης έκανε το δικό του F5 restart στο VS για browser testing.

---

## 8. Τρέχουσα κατάσταση (τι δουλεύει)

- Core (Domain/Application/Infrastructure) πλήρες & tested (20 tests).
- Loyalty DB δημιουργείται & seed-άρεται στο boot. **Demo seed (fixed Guids, ανεξάρτητο από πραγματικά members):**
  - `DemoMemberAlpha = a1a1a1a1-0000-0000-0000-000000000001` → 750 πόντοι (500 welcome + 250 sale)
  - `DemoMemberBeta  = b2b2b2b2-0000-0000-0000-000000000002` → 120 πόντοι
- 3 marketing σελίδες render σωστά με δυναμικό nav: Home, How it works, Contact (URLs τώρα culture-specific, βλ. Phase 6 παρακάτω).
- **Members & Authentication (Phase 3) — verified end-to-end:**
  - Member Type `kioskOwner` (`kioskName`, `region`) + Member Group `Kiosk Owners`.
  - Test member `KioskKarlovasi` (Key `6c7a2679-7dbe-4fa8-85be-43e594ab10e6`) — login δουλεύει, welcome bonus **100 πόντοι** επιβεβαιωμένο στο log.
  - Public Access στο Dashboard restrict σε `Kiosk Owners`.
  - Header widget: ανώνυμος → Login link· συνδεδεμένος → My dashboard | Hi, {name} | Logout (πλέον dictionary-driven, βλ. Phase 6).
- **Member Dashboard (Phase 4) — verified end-to-end:** `/dashboard/` (ή culture-equivalent) route-hijacked → πραγματικό balance + πίνακας συναλλαγών.
- **Rewards Catalog & Redemption (Phase 5) — verified end-to-end:**
  - Document Types `Reward` (Title, Description, PointsCost, Image) + `RewardsCatalog` (container).
  - Content: 3 test rewards — `Lighter` (50 πόντοι), `Ashtray` (150), `Giftcard 20 Euro` (500).
  - `Views/RewardsCatalog.cshtml` — convention rendering, `@inject IMemberManager` + `@inject IPointsService`. `RewardsController : SurfaceController` → `POST Redeem(Guid rewardKey)`.
  - **Σημ.:** το live balance του `KioskKarlovasi` είναι **50** (μετά από test redeem του Lighter, -50 από τα 100 welcome points).
- **Πολυγλωσσικότητα (Phase 6) — verified end-to-end, και στις δύο γλώσσες:**
  - **Languages:** English (United States, en-US) + Greek (Greece, el-GR)· **Greek = system Default** (flag αλλαγμένο ρητά, βλ. gotcha #16), English = δεύτερη γλώσσα.
  - **Routing:** `Home` node → Culture and Hostnames: Culture = Greek (root, χωρίς prefix), Hostnames = `/en` → English. Παιδιά = Inherit. Κάθε σελίδα έχει το δικό της culture-specific URL slug (π.χ. `/κατάλογος-επιβραβεύσεων/` GR, `/en/rewards-catalog/` EN — τα ακριβή slugs εξαρτώνται από το Name που έδωσε ο editor ανά culture).
  - **Vary by culture ενεργό (Document Type + per-property "Shared across cultures" OFF) σε:** `HomePage` (heading, bodyText), `ContentPage` (heading, bodyText), `Reward` (Title, Description — `PointsCost`/`Image` παραμένουν invariant). **Ανοιχτό/μη-επιβεβαιωμένο ρητά:** αν το `SEO` composition (metaTitle/metaDescription) πήρε το ίδιο toggle — άξιζε ένα γρήγορο spot-check στην αρχή του επόμενου session.
  - **Περιεχόμενο μεταφρασμένο:** Home, How it works, Contact (heading/bodyText), τα 3 Rewards (Title/Description) — GR/EN ξεχωριστά, χωρίς πλέον cross-culture sync bug.
  - **Republish έγινε ρητά (Save & Publish και τα δύο cultures) σε:** How it works, Contact, Rewards Catalog, τα 3 Rewards, Login, Dashboard — απαραίτητο μία φορά μετά την ενεργοποίηση 2ης γλώσσας (gotcha #13).
  - **26 Dictionary items** (Settings→Translation), οργανωμένα flat με dot-prefixed keys (π.χ. `Login.Heading`) — ΟΧΙ σε folders (θα άλλαζε το lookup key, βλ. απόφαση στο session):
    - `Header.Greeting`, `Header.Login`, `Header.Logout`, `Header.MyDashboard`
    - `Login.Heading`, `Login.Subtitle`, `Login.UsernameLabel`, `Login.PasswordLabel`, `Login.RememberMeLabel`, `Login.SubmitButton`, `Login.ErrorLockedOut`, `Login.ErrorNotAllowed`, `Login.ErrorInvalidCredentials`
    - `Dashboard.PointsBalanceLabel`, `Dashboard.TransactionHistoryHeading`, `Dashboard.NoTransactionsYet`, `Dashboard.ColumnDate`, `Dashboard.ColumnType`, `Dashboard.ColumnDescription`, `Dashboard.ColumnAmount`, `Transaction.TypeEarn`, `Transaction.TypeRedeem`
    - `Rewards.YourBalancePrefix`, `Rewards.PointsSuffix`, `Rewards.NoRewardsAvailable`, `Rewards.RedeemButton`, `Rewards.LoginToRedeem`, `Rewards.RedeemNotFound`, `Rewards.RedeemSuccess` (περιέχει literal `{0}`/`{1}` tokens, γεμίζονται με `string.Format` στον `RewardsController`)
    - `Footer.Tagline`
  - **Σκόπιμα ΟΧΙ μεταφρασμένα** (τεκμηριωμένη απόφαση, όχι αμέλεια): `[Required(ErrorMessage=...)]` στο `LoginFormModel.cs` (ξεχωριστό ASP.NET Core DataAnnotations localization σύστημα, εκτός scope)· το `result.Error` message από `IPointsService.RedeemAsync` (domain-layer, culture-agnostic per §4).
  - **Πραγματικό language switcher** στο `_Layout.cshtml` header (EL/EN links, δουλεύει ανά-σελίδα — πάει στο *ίδιο* node στην άλλη γλώσσα, όχι πάντα στην αρχική). `<html lang="...">` δυναμικό πλέον.
  - **3 hardcoded-path bugs βρέθηκαν & διορθώθηκαν** (βλ. gotcha #17): `_LoginStatus.cshtml`, `RewardsCatalog.cshtml`, `LoginPage.cshtml`.

---

## 9. ΕΠΟΜΕΝΟ — Phase 7: Stretch (ΠΡΟΣΧΕΔΙΟ — να διαλέξουμε κατεύθυνση πριν ξεκινήσουμε)

> ⚠️ Το Phase 7 ήταν πάντα ένα γενικό «stretch» label στο roadmap (§3), χωρίς detail plan σαν το Phase 6 (§9 παλιά). **Στην αρχή του επόμενου session, διάλεξε μαζί με τον χρήστη ΠΟΙΑ από τις παρακάτω κατευθύνσεις (ή ποιος συνδυασμός/σειρά) θέλει**, πριν γράψεις κώδικα — ίδιο μοτίβο με το Phase 6.

Τρεις υποψήφιες κατευθύνσεις (από το αρχικό roadmap):

1. **Examine search** — Umbraco's built-in Lucene-based indexing (`Examine`). Θα πρόσθετε ένα search box (π.χ. εύρεση rewards/σελίδων by keyword). Concepts: custom index fields, `ISearcher`, index rebuild.
2. **JSON API** — έκθεση ενός κομματιού του loyalty/rewards data ως JSON endpoint (π.χ. μέσω του ήδη ενεργού Umbraco **Delivery API**, ή ενός custom `ApiController`). Θα προσομοίωνε ένα μελλοντικό kiosk-terminal/SPA client που καταναλώνει τα ίδια δεδομένα. Concepts: API layer πάνω από το υπάρχον `IPointsService`, πιθανώς authentication για API calls (διαφορετικό από member cookie auth).
3. **uSync** — community package για export/import Document Types + settings σε αρχεία (version-controllable). Μέχρι τώρα ΟΛΗ η σχηματική δουλειά (Document Types, Templates, Languages, Dictionary items) έγινε αποκλειστικά μέσω backoffice UI, χωρίς κανένα αρχείο να αντιπροσωπεύει το schema στο git — αυτό θα εισήγαγε μια νέα, πρακτικά χρήσιμη κατηγορία concept (deployment/devops για Umbraco schema) που δεν έχουμε αγγίξει καθόλου ακόμα.

Δεν χρειάζεται να γίνουν όλα· ίσως αξίζει να διαλέξετε 1-2 από τα 3 ανάλογα με το τι θέλει να μάθει περισσότερο ο χρήστης (search/API feature-building vs. devops/schema-versioning).

### Πριν ξεκινήσετε το Phase 7, μικρό open item από το Phase 6:
- Spot-check αν το `SEO` composition (metaTitle/metaDescription) πήρε "Vary by culture" per-property (βλ. §8) — αν όχι, quick backoffice fix (ίδιο drill με Reward/ContentPage/HomePage, §5 «δύο-επίπεδο toggle»).

---

## 10. File map (πού είναι τι)

**Domain:** `Entities/PointsAccount.cs`, `Entities/PointsTransaction.cs`, `Enums/TransactionType.cs`, `Common/Result.cs`, `Exceptions/DomainException.cs`, `Exceptions/InsufficientPointsException.cs`
**Application:** `Abstractions/IPointsService.cs`, `DTOs/PointsTransactionDto.cs`
**Infrastructure:** `Persistence/LoyaltyDbContext.cs`, `Persistence/LoyaltyDbContextFactory.cs`, `Persistence/Configurations/{PointsAccount,PointsTransaction}Configuration.cs`, `Persistence/Migrations/*`, `Services/PointsService.cs`, `Seeding/LoyaltyDataSeeder.cs`, `DependencyInjection/InfrastructureServiceRegistration.cs`
**Tests:** `Domain/PointsAccountTests.cs`, `Services/SqliteTestBase.cs`, `Services/PointsServiceTests.cs`
**Web:**
- `Composers/LoyaltyComposer.cs`, `Composers/LoyaltyStartupHandler.cs`, `Composers/MemberSavedLoyaltyHandler.cs` (auto-provision loyalty account on member save)
- `Controllers/MemberAuthController.cs` (login/logout Surface Controller — πλέον injects `ICultureDictionaryFactory` για τα 3 sign-in error messages, βλ. Phase 6)
- `Controllers/MemberDashboardController.cs` (route-hijacking Render Controller, alias `memberDashboard`)
- `Controllers/RewardsController.cs` (redeem Surface Controller — πλέον injects `ICultureDictionaryFactory` για not-found/success messages· `result.Error` deliberately αμετάφραστο)
- `Models/LoginFormModel.cs`, `Models/MemberDashboardViewModel.cs` (extends generated `MemberDashboard`)
- `Views/_Layout.cshtml` (dynamic `<html lang>`, footer dictionary lookup, "Home" link = `@home.Name`, **language switcher** μέσω `Model.Cultures.Keys` + `.Url(culture:)`)
- `Views/HomePage.cshtml`, `Views/ContentPage.cshtml` (100% content-driven, καμία hardcoded string — δεν χρειάστηκαν αλλαγή στο Phase 6)
- `Views/LoginPage.cshtml` (heading/subtitle dictionary-driven· `returnUrl` fallback πλέον culture-aware μέσω `.DescendantsOrSelf<MemberDashboard>().Url()`)
- `Views/MemberDashboard.cshtml` (real balance + history table· labels/columns/transaction-type dictionary-driven· h1 = `@Model.Name`)
- `Views/RewardsCatalog.cshtml` (convention-rendered catalog + redeem form· labels/messages dictionary-driven· h1 = `@Model.Name`· "Login to redeem" link culture-aware)
- `Views/Partials/_LoginForm.cshtml` (`@inject UmbracoHelper Umbraco`· labels κρατούν `asp-for` για accessibility αλλά με δικό τους dictionary-driven κείμενο)
- `Views/Partials/_LoginStatus.cshtml` (`@inherits UmbracoViewPage`· My dashboard/Login/Logout links πλέον resolve το πραγματικό node + `.Url()` αντί για hardcoded paths· dictionary-driven text)
- `appsettings.Development.json` (LoyaltyDb conn)
- `umbraco/models/*.generated.cs` (incl. `KioskOwner.generated.cs`, `Reward.generated.cs`, `RewardsCatalog.generated.cs`)

---

## 11. Working style (preferences)

- Φάση-φάση, με εξήγηση concepts (ο χρήστης μαθαίνει Umbraco — όχι copy-paste).
- Πριν από φάση χωρίς προσυμφωνημένο detail plan (π.χ. Phase 6, τώρα Phase 7): περάστε πρώτα τις βασικές αποφάσεις μαζί, ΜΕΤΑ κώδικας.
- Verify πραγματικά (build + tests + boot/curl) πριν πεις «έγινε».
- Code comments στα Αγγλικά (αποφυγή encoding issues), συζήτηση στα Ελληνικά.
- Ο χρήστης κάνει τα backoffice clicks (για να μάθει)· το AI γράφει τον κώδικα.
- Σε bugs/αναπάντεχη συμπεριφορά: ζήτα screenshot + ακριβές URL/βήματα πριν μαντέψεις αιτία — στο Phase 6 πολλά "bugs" ήταν στην πραγματικότητα testing του λάθος URL (π.χ. `/login/` αντί `/σύνδεση/`) ή αναμενόμενο (γιατί δεν βοήθησε ένα toggle που φαινόταν σωστό αλλά ήταν μισό-καμωμένο).
