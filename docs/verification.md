# Vérifications du 24 septembre 2026

## Périmètre actuel

Clients, banques, comptes bancaires, authentification et administration des utilisateurs.
Les anciens modules documentaires sont retirés. Les migrations historiques sont
conservées pour assurer la mise à jour des bases existantes.

## Contrôles effectués

- Backend : compilation réussie ; 10 tests réussis, aucun échec.
- Intégration PostgreSQL : CRUD clients/comptes/banques, images, droits et suppressions
  détaillées avec détection des modifications concurrentes.
- Administration : création, doublons, validation des permissions et dépendances,
  lecture seule, délégation des écritures bancaires, élévation/rétrogradation,
  désactivation/réactivation, réinitialisation de mot de passe, révocation et suppression.
- Refus des endpoints d'administration pour User, protection de son propre compte
  et du dernier administrateur actif, rejet des versions de modification périmées.
- Les anciens JWT sont refusés après changement, désactivation, réinitialisation,
  révocation ou suppression. Les clients et banques survivent à la suppression de l'utilisateur.
- La suite d'administration utilise une base temporaire isolée, supprimée en fin de test.
- Migration UserAdministration appliquée à la base locale ; les deux administrateurs
  existants restent actifs. Aucun changement de modèle en attente signalé par EF Core.
- API locale : connexion, identité courante, liste des 2 utilisateurs, catalogue des
  10 permissions et contrôle du client HTTP TypeScript réussis.
- Données de l'application : 6 clients, 7 banques, 2 comptes ; images et tableau de bord lisibles.
- Frontend : ESLint, TypeScript et build de production réussis ; route /users incluse.
- Réponse HTTP 200 de la page /users.

## Limites de vérification

Aucun navigateur connecté n'était disponible pour une recette visuelle interactive.
Le rendu mobile et les clics dans les boîtes de dialogue restent à vérifier manuellement.
Docker et déploiement public n'ont pas été testés dans cette session.

## Reproduire

Avec une base PostgreSQL dédiée aux tests et un rôle disposant de CREATEDB :

```powershell
$env:TEST_DATABASE_URL = 'Host=127.0.0.1;Port=55432;Database=bankconverter_tests;Username=converter'
dotnet test BankStatementConverter.sln --no-restore
```

Le cluster .local/pgdata est uniquement destiné aux tests. La configuration privée
de l'application demeure dans .local/dev-secrets.json. Le démarrage local utilise
scripts/start-backend.ps1. Ne jamais publier les secrets ni utiliser une base réelle
pour les tests d'intégration.

Dans frontend/Import-pdf-excel-master : npm run lint et npm run build.
Pour le contrôle API en lecture seule : définir SMOKE_EMAIL et SMOKE_PASSWORD,
puis lancer node scripts/smoke-catalogs.mjs.

## Refonte visuelle — 24 septembre 2026

- Identité ALIAS : logos complets et compacts des assets, variantes clair/sombre.
- Navigation latérale, en-tête et présentation des comptes modernisés.
- Comptes : vue cartes/liste, copie du numéro, confirmation de suppression,
  états vide/chargement/erreur et conservation des autorisations.
- ESLint : réussi sans erreur. Compilation de production et TypeScript : réussis.
- Aucun navigateur disponible via l’outil de contrôle : validation visuelle
  interactive des deux thèmes et des formats mobiles encore à effectuer.

## Menu compact et biblioth?ques UI ? 24 septembre 2026

- Installation HeroUI 3.2.6, Motion et utilitaires : audit npm sans vuln?rabilit?.
- ESLint, TypeScript et build de production r?ussis.
- ? contr?ler dans un navigateur : r?duire/d?velopper le menu, recharger pour
  v?rifier la m?morisation, parcourir les liens au clavier et lire les infobulles,
  basculer les th?mes, ouvrir/fermer le menu mobile et activer la r?duction des
  animations. Les ?crans de comptes doivent conserver les vues cartes/liste et
  les actions autoris?es selon le r?le.
- Aucun navigateur connect? disponible pour ex?cuter cette recette visuelle.

## Monochromatic Minimalism et composants partagés

- Palette claire/sombre centralisée ; logos affichés en niveaux de gris.
- Button, Input, Textarea, Card, Badge, Skeleton et Tooltip shadcn/ui intégrés.
- Motion : pages, cartes, connexion, dialogues et menu ; réduction des animations.
- HeroUI retiré ; audit npm : aucune vulnérabilité signalée.
- Build et TypeScript réussis. Pages login, dashboard, clients, banks,
  bank-accounts, users et settings : HTTP 200 en développement.
- Contrôle de structure : les huit usages de Card asChild ont un enfant unique.
- La validation HTTP ne remplace pas la recette authentifiée dans un navigateur.
  Rendu des deux thèmes, formulaires et animations à vérifier visuellement.
