# ReleveFlow

Gestion des clients, des banques et des comptes bancaires pour un cabinet partagé.

## Fonctionnalités

- Connexion, inscription, rôles Admin/User et sessions JWT.
- Administration : utilisateurs, rôles, permissions, activation, suppression,
  réinitialisation du mot de passe et déconnexion forcée.
- Clients : coordonnées, identité fiscale, photos et suppression avec comptes associés.
- Banques : recherche, tri, logos, détails des comptes ; modifications selon permissions.
- Comptes : client, banque, numéro, devise, journal et compte comptable.
- Tableau de bord avec les trois compteurs réels ; profil utilisateur et thème clair/sombre.

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

## Docker

Copier `.env.example` vers `.env`, définir les secrets, puis
`docker compose up --build -d`. Le volume `pgdata` conserve les données.
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
| GET /health | Santé API/base |

Listes : `page` et `pageSize` (maximum 100). Banques : `search`,
`sort=name|name-desc|code`. Swagger est disponible en développement.

## Validation et maintenance

Voir [les vérifications](docs/verification.md) et [l'architecture](docs/architecture.md).
Les données de test nécessitent une base distincte de celle de l'application.
Les sauvegardes doivent inclure PostgreSQL, où sont également stockées les images.
Pour retirer les caches de compilation : arrêter les serveurs, puis exécuter
`powershell -File scripts/clean-cache.ps1`. Conserver les fichiers de verrouillage
des dépendances et les migrations pour des installations reproductibles.
