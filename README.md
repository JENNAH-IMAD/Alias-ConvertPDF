# ReleveFlow

Gestion des clients, des banques, des comptes bancaires et des relevés pour un cabinet partagé.

## Fonctionnalités

- Connexion, inscription, rôles Admin/User et sessions JWT.
- Administration : utilisateurs, rôles, permissions, activation, suppression,
  réinitialisation du mot de passe et déconnexion forcée.
- Clients : coordonnées, identité fiscale, photos et suppression avec comptes associés.
- Banques : recherche, tri, logos, détails des comptes ; modifications selon permissions.
- Comptes : client, banque, numéro, devise, journal et compte comptable.
- Tableau de bord avec les trois compteurs réels ; profil utilisateur et thème clair/sombre.
- Conversions : dépôt PDF privé, analyse asynchrone, extraction par profil ou OCR,
  revue manuelle, contrôle des soldes et validation avant export.
- Modèles d'export CSV, Sage 100 et Sage X3 configurables ; exports conservés.
- Espace client et archives filtrables avec PDF original, opérations et historique.
- Maximum cinq relevés conservés par client, avec compteur dès l’import et protection contre les imports simultanés.

Voir le [guide des relevés, exports et archives](docs/statements.md) pour le parcours,
la configuration OCR, les profils bancaires et les limites de prise en charge.

## Architecture

Next.js 16 / React 19 / TypeScript / Tailwind CSS ; API ASP.NET Core 8 ;
PostgreSQL et EF Core. Les contrôleurs utilisent les contrats Application,
les services Infrastructure implémentent les opérations métier et Domain porte
les entités. Voir [architecture et organisation](docs/architecture.md).

## Démarrage local

Prérequis : .NET 8 compatible, Node.js 24, PostgreSQL 16 ou supérieur.

1. Configurer `ConnectionStrings__Default`, `Jwt__Key` (32 octets minimum),
   `Seed__AdminEmail`, `Seed__AdminPassword` et `Cors__Origins__0`.
2. Appliquer les migrations avec `Database__AutoMigrate=true` au démarrage,
   ou avec `dotnet tool run dotnet-ef database update --project backend/BankStatementConverter.Infrastructure --startup-project backend/BankStatementConverter.API`.
3. `dotnet run --project backend/BankStatementConverter.API --urls http://localhost:5080`.
4. Dans `frontend/Import-pdf-excel-master` : `npm ci`, puis `npm run dev`.

Dans cet espace local, `scripts/start-backend.ps1` utilise la configuration privée
existante `.local/dev-secrets.json`. Ne jamais publier ce fichier.
L'interface est sur http://localhost:3001, l'API sur http://localhost:5080.
Le fichier `.env` n'est pas lu automatiquement par .NET.

### Fichier DLL verrouillé au lancement (MSB3027 / MSB3021)

Une instance du backend est encore active. Arrêter son terminal avec `Ctrl+C`
avant de relancer `dotnet run`. Si l'instance tourne en arrière-plan, depuis la racine :

```powershell
./scripts/stop-backend.ps1
./scripts/start-backend.ps1
```

Ou utiliser `./scripts/start-backend.ps1 -Restart`. Ces scripts ciblent uniquement
l'exécutable API de ce projet. Le démarrage reste dans le terminal courant et
refuse un double lancement avant la compilation. Il ne faut pas lancer en parallèle
ce script et `dotnet run` dans un deuxième terminal. Supprimer `bin`/`obj` n'est
pas nécessaire pour résoudre ce verrouillage.

## Docker

Copier `.env.example` vers `.env`, définir les secrets, puis
`docker compose up --build -d`. Le volume `pgdata` conserve les données et
le volume `statements` conserve les PDF et exports privés. L'image API installe
Poppler et Tesseract avec les langues française et anglaise.
Les ports sont publiés sur localhost. L'adresse `NEXT_PUBLIC_API_URL` est intégrée
au build frontend et demande une recompilation si elle change.

## API

| Routes | Usage |
| --- | --- |
| POST /api/auth/login, /api/auth/register | Session et inscription |
| /api/clients, /api/banks, /api/bank-accounts | Listes et CRUD |
| GET /api/banks/{id}/accounts | Comptes associés |
| /api/clients/{id}/photo, /api/banks/{id}/logo | GET, PUT multipart et DELETE |
| /api/catalog-deletions/{clients ou banks}/{id} | GET récapitulatif, POST confirmation |
| GET /api/dashboard | Compteurs clients, banques et comptes |
| /api/users | Administration des utilisateurs (Admin uniquement) |
| GET /api/auth/me | Identité et permissions courantes |
| /api/statements, /api/archives | Relevés et archives accessibles à l'utilisateur |
| POST /api/bank-accounts/{id}/statements/upload | Dépôt PDF |
| /api/export-templates | Modèles d'export ; modifications Admin |
| /api/statement-profiles | Consultation et création Admin de profils bancaires |
| GET /health | Santé API/base |

Listes : `page` et `pageSize` (maximum 100). Banques : `search`,
`sort=name|name-desc|code`. Swagger est disponible en développement.

## Validation et maintenance

Voir [les vérifications](docs/verification.md) et [l'architecture](docs/architecture.md).
Les données de test nécessitent une base distincte de celle de l'application.
Les sauvegardes doivent inclure PostgreSQL, où sont également stockées les images,
et le répertoire `Statements__StoragePath` (ou `private-statements` dans le dossier
API par défaut). En Docker, sauvegarder les deux volumes `pgdata` et `statements`.
Pour retirer les caches de compilation : arrêter les serveurs, puis exécuter
`powershell -File scripts/clean-cache.ps1`. Conserver les fichiers de verrouillage
des dépendances et les migrations pour des installations reproductibles.
