# Architecture de ReleveFlow

L'application gère les référentiels d'un cabinet et les relevés bancaires,
depuis le dépôt PDF jusqu'aux exports et aux archives.

```text
frontend/Import-pdf-excel-master/src/
  app/           Pages : connexion, inscription, tableau de bord,
                 clients, banques, comptes, utilisateurs et paramètres,
                 conversions, détail relevé, modèles, espace client, archives
  components/    Formulaires, dialogues, avatars et thèmes
  services/      Client HTTP typé et chargement des données

backend/
  BankStatementConverter.Domain/          Entités métier
  BankStatementConverter.Application/     DTO et interfaces de services
  BankStatementConverter.Infrastructure/
    Catalogs/       Gestion des référentiels et tableau de bord
    Identity/       Authentification, utilisateurs, permissions et sessions
    Media/          Photos des clients et logos des banques
    Persistence/    DbContext et initialisation des données
    Statements/     Stockage privé, PDF/OCR, extraction, revue, exports et file de traitement
    Migrations/     Historique des évolutions du schéma
  BankStatementConverter.API/
    Controllers/    Adaptation HTTP et autorisations
    Program.cs      Assemblage des dépendances et middlewares
  BankStatementConverter.Tests/            Validation et intégration HTTP
scripts/                                  Démarrage, contrôle et nettoyage
```

## Dépendances

Domain ne dépend pas des autres couches. Application référence Domain.
Infrastructure implémente les contrats Application et utilise EF Core/Npgsql.
Les contrôleurs des référentiels dépendent des interfaces Application.
Les contrôleurs des relevés utilisent les services Infrastructure et les contrats
Application ; le contrôleur de profils utilise actuellement le DbContext.
Le stockage, l'analyse PDF, l'OCR, la validation et l'export disposent d'interfaces
substituables. Les images des référentiels restent en PostgreSQL ; les PDF et exports
sont conservés dans un stockage privé, servi uniquement par des routes autorisées.

Les noms techniques des projets sont conservés pour les outils de lancement existants.

## Données

Référentiels : Users, Clients, Banks et BankAccounts.
Relevés : BankStatements, BankTransactions, BankStatementProfiles,
ExportTemplates, ExportTemplateFields, StatementExports et ProcessingHistories.
Les utilisateurs partagent les référentiels selon leurs permissions. Admin dispose
de tous les droits et gère les utilisateurs. Les photos clients et logos sont limités
à 2 Mo. Les comptes conservent leur devise, journal et compte comptable.

## Administration des utilisateurs

`/users` est réservé à Admin : liste paginée, recherche par nom/e-mail, filtres
rôle/état, création, modification, suppression, activation/désactivation, rôle
Admin/User, permissions, nouveau mot de passe et révocation des sessions.
La recherche est temporisée ; la liste propose actualisation et remise à zéro
des filtres. Après suppression du dernier élément d'une page, elle revient à la
page précédente. La création et la réinitialisation demandent deux saisies
identiques du mot de passe pour éviter les erreurs de frappe.
Les droits `read`, `write` (création/modification/images), `delete` s'appliquent
aux clients, banques et comptes. Le tableau de bord possède `dashboard.read`.
La gestion des utilisateurs n'est jamais délégable par une simple permission.

Un filtre d'autorisation serveur couvre toutes les routes des référentiels,
y compris photos, logos et suppressions détaillées. Les écritures nécessitent
la consultation du module. Modifier des comptes nécessite la consultation des
clients et banques ; supprimer un client/une banque avec dépendances nécessite
aussi `accounts.delete`. L'interface masque les pages interdites et limite les actions.

La migration `UserAdministration` garde les comptes existants actifs. Une valeur
Permissions NULL conserve les droits historiques par défaut ; une chaîne vide
signifie aucun droit. Les nouveaux inscrits restent User avec ces droits par défaut.
L'inscription publique existante reste disponible ; aucun utilisateur existant
n'est promu automatiquement.

Chaque requête authentifiée vérifie en base l'existence, l'état et SessionVersion
du compte. Les changements, réinitialisations et révocations invalident les anciens
jetons dès la requête suivante. Le frontend actualise `/api/auth/me` toutes les
15 secondes. Les JWT sans version ne restent valides que pour une version initiale 0.

Les changements d'utilisateurs sont transactionnels. Les protections contre la
suppression du dernier Admin actif et l'auto-suppression/désactivation/rétrogradation
sont exécutées sous verrou de la table Users. UpdatedAt sert de version de modification
(précision PostgreSQL en microsecondes). La suppression d'un utilisateur ne supprime
aucun client, banque ou compte. Les mots de passe ne sont jamais retournés par l'API.

Routes Admin : GET/POST `/api/users`, PUT/DELETE `/api/users/{id}`,
GET `/api/users/permissions`, PUT `/api/users/{id}/password`,
POST `/api/users/{id}/revoke-sessions`. GET `/api/auth/me` renvoie l'identité courante.

Les suppressions détaillées retirent un client ou une banque et les comptes
associés, dans une transaction, après comparaison de la version du récapitulatif.
Supprimer une banque préserve les clients ; supprimer un client préserve les banques.

## Migrations

Les migrations historiques sont nécessaires pour mettre à jour les installations
existantes et reconstruire une base neuve. La migration RemoveDocumentProcessing
supprime exclusivement les cinq tables des fonctionnalités retirées. Elle ne
modifie aucune ligne des quatre tables métier conservées. Les anciens noms ne
subsistent que dans cet historique technique ; aucun endpoint ou moteur associé
n'est réactivé. Les migrations additives `StatementWorkspace` et
`StatementIdentifiers` installent le nouveau modèle documentaire. Elles ne
recréent aucune banque, aucun client ni compte fictif. Ne pas effacer les migrations
déjà appliquées. Le [guide documentaire](statements.md) décrit le nouveau moteur.

## Vérifications

- `dotnet test BankStatementConverter.sln --filter 'Category!=Integration'`
- Pour l'intégration, définir `TEST_DATABASE_URL` sur une base dédiée, puis
  `dotnet test BankStatementConverter.sln`.
  Le test d'administration crée et supprime une base isolée : le rôle PostgreSQL
  de test nécessite CREATEDB. Il ne faut jamais utiliser une connexion de production.
- Dans le frontend : `npm run lint` et `npm run build`.
- Avec l'API démarrée : définir `SMOKE_EMAIL` et `SMOKE_PASSWORD`, puis
  `node scripts/smoke-catalogs.mjs`. Ce contrôle ne modifie aucun référentiel.

`scripts/clean-cache.ps1` supprime les sorties de compilation lorsque les serveurs
sont arrêtés. Il conserve les sources, node_modules et les bases de données.

## Identité visuelle et composants

La palette Monochromatic Minimalism est centralisée dans `app/monochrome.css`.
Clair : fond et surfaces #E0E0E0, texte et actions #121212, surfaces secondaires #B0B0B0, bordures #888888 et texte secondaire #444444.
Sombre : fond et surfaces #121212, texte et actions #E0E0E0, texte secondaire #B0B0B0, bordures et surfaces secondaires #444444, focus #888888.
Les erreurs et suppressions conservent une couleur sémantique distincte.
Le composant Brand recolore le logo et le symbole via des filtres SVG : #E0E0E0 sur #121212 en sombre, couleurs inversées en clair. Les fichiers des assets restent intacts.

Les composants shadcn/ui locaux comprennent Button, Input, Textarea, Card,
Badge, Skeleton et Tooltip. Ils sont utilisés dans les pages et formulaires.
HeroUI a été retiré des dépendances après migration des derniers boutons.
Les sélecteurs, confirmations et dialogues natifs préservent la validation HTML.

Motion anime les pages, cartes, dialogues, connexion et menu. Blur Fade provient
 de Magic UI ; sa licence accompagne le fichier. Les composants Card et le menu
respectent `useReducedMotion`, avec une politique globale `MotionConfig`.
Les effets CSS respectent également la réduction des animations.

La palette neutre est désormais complétée d'accents bleus pour les actions et
la navigation, et de vert/ambre/rouge pour les états des relevés. Les libellés et
icônes complètent la couleur. Le menu conserve le logo et le profil visibles
pendant le défilement de ses liens ; la vue d'ensemble apparaît en premier.

`components/ui/sidebar.tsx` compose Button et Tooltip avec Motion. Le bouton
expose `aria-expanded`. `use-compact-sidebar.ts` mémorise le choix dans
`alias-sidebar-compact` et prévoit un repli si le stockage est indisponible.
Le tiroir mobile reste indépendant. Le mode compact affiche le symbole A.

Sources : https://ui.shadcn.com/docs/components,
https://magicui.design/docs/components/blur-fade et https://motion.dev/docs/react.
Les licences locales figurent dans `src/components/ui`.

La refonte visuelle ne change pas les règles métier. Le module documentaire ajoute
les permissions `statements.read` et `statements.write`, les routes et tables
décrites dans le [guide](statements.md).

L'archive client est limitée à cinq relevés importés, tous comptes et auteurs confondus.
Une transaction verrouille le client avant de compter et d'importer. La migration
de données `FrenchExportPresets` installe les trois modèles demandés sans modifier
les référentiels ni les modèles existants.


