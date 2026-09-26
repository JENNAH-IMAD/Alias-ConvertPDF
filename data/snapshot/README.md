# Copie des donnees du 26 septembre 2026

`convertbank.dump` contient le schema PostgreSQL et toutes les lignes de la base actuelle, y compris les utilisateurs (mots de passe haches), clients, comptes, logos, photos, profils, modeles et historiques. `files/` contient les PDF et exports du stockage local. Les secrets de configuration `.env`, JWT et PostgreSQL ne sont pas inclus.

## Sur un autre PC

Installer Git et Docker Desktop (moteur Linux), puis :

```sh
git clone --branch version-4 https://github.com/JENNAH-IMAD/Alias-ConvertPDF.git
cd Alias-ConvertPDF
```

Copier `.env.example` vers `.env`. Renseigner `POSTGRES_PASSWORD` avec un nouveau mot de passe PostgreSQL et `JWT_KEY` avec une nouvelle cle aleatoire d'au moins 32 caracteres. `ADMIN_EMAIL` et `ADMIN_PASSWORD` ne sont pas utilises par ce lancement : les comptes existants sont restaures.

```sh
docker compose -f docker-compose.snapshot.yml up --build -d
```

Ouvrir http://localhost:3002 et se connecter avec les memes identifiants que sur le PC source.

La restauration s'effectue automatiquement sur les volumes neufs uniquement. Les redemarrages conservent ensuite les modifications. Ce lancement utilise PostgreSQL 18 et des volumes distincts du compose standard. Arreter une autre instance de l'application utilisant deja les ports 3002/5081 avant de le lancer.

Il s'agit d'une photographie des donnees a la date indiquee, pas d'une synchronisation entre PC. Un `git pull` ne remplace pas une base deja initialisee. Ne pas supprimer les volumes pour mettre a jour l'application.

## Sans Docker

Avec PostgreSQL 18, creer une base vide puis restaurer (remplacer les noms d'utilisateur/base selon votre installation) :

```sh
createdb -h localhost -U postgres convertbank
pg_restore -h localhost -U postgres -d convertbank --no-owner --no-acl --exit-on-error --single-transaction data/snapshot/convertbank.dump
```

Copier le contenu de `data/snapshot/files/` dans le dossier `uploads/` a la racine du projet. Configurer `Storage__Root` avec le chemin absolu de ce dossier et `ConnectionStrings__Default` avec la connexion a la base restauree. Suivre ensuite le lancement local du README principal, avec `Seed__DemoData=false` et sans configurer de nouvel administrateur. Utiliser une nouvelle cle JWT.
