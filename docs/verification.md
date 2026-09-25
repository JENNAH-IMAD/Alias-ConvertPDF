# Vérifications — modules documentaires

## Ajustements français, modèles, archives et navigation

### Thèmes et accès aux fichiers — 25 septembre

- Correction du texte hérité des boutons principaux sur les boutons secondaires.
  Couleurs explicites pour les boutons secondaires, contours, transparents et états.
- Contrôle numérique des 14 paires texte/fond principales : contraste minimal
  5,56:1 ; texte secondaire, surfaces, actions et survols testés dans les deux thèmes.
  Ce calcul ne remplace pas une recette visuelle de tous les composants.
- Choix Sage 100 / Sage X3 / CSV placé en deuxième étape après import.
- Consultation PDF par blob authentifié ; révocation de l'URL et annulation de la
  requête à la fermeture. Aucun jeton n'est ajouté à l'URL du document.
- CSV de travail consultable/téléchargeable avant validation et après archivage.
  Les noms distinguent les données à vérifier des données validées.
- Aperçus des exports finaux UTF-8/Windows-1252 testés avec les accents français.
- Refus des aperçus et téléchargements pour un autre utilisateur : HTTP 404.
- Suite backend : 18 tests réussis. Aucun changement de schéma requis.
- Archives et Espace clients gardent leurs routes et utilisent la même liste de relevés.
- L'Espace clients est désormais limité aux archives du client sélectionné. Les cartes
  affichent PDF original, nombre d'opérations converties et nombre d'exports finaux.
  Le test d'intégration vérifie ces deux compteurs dans la réponse des archives.
- Correction de l'avertissement React sur les clés dupliquées : le compteur
  d'archives et la liste des relevés utilisent désormais des clés distinctes,
  y compris lorsqu'aucun compte bancaire n'est sélectionné.
- Aucun navigateur connecté disponible pour vérifier visuellement les fenêtres PDF,
  la navigation au clavier, le mobile et les deux thèmes.

- Statuts, étapes de l'historique et sources des colonnes traduits en français.
  Les erreurs de validation et de configuration d'export sont enregistrées avec leur statut.
- Migration `FrenchExportPresets` appliquée : les 3 modèles français sont présents,
  avec respectivement 6 colonnes Sage 100, 7 Sage X3 et 5 CSV personnalisé.
- Tests d'intégration : installation unique des modèles, erreur de validation,
  échec d'export historisé et reprise, limite de 5 relevés conservés par client.
- Deux imports simultanés sur deux comptes du même client à 4 relevés :
  exactement une réussite et un refus HTTP 409. Un autre client reste indépendant.
- 18 tests backend réussis. Compilation Debug : zéro erreur et zéro avertissement.
  Frontend : ESLint, TypeScript et build de production réussis.
- Les référentiels restent inchangés après migration : 2 utilisateurs, 3 clients,
  9 banques et 1 compte, photos et logos compris.
- Vue d'ensemble placée en tête ; accents bleus et couleurs d'état ; défilement
  natif du menu, des dialogues et de l'application. Réduction des animations respectée.
- Le backend est laissé arrêté pour éviter de verrouiller les DLL au prochain lancement.
  La recette visuelle authentifiée des deux thèmes reste à effectuer.

## Correction du double lancement local

Le processus API 8984, resté actif après une vérification, verrouillait les DLL
Debug et provoquait MSB3027/MSB3021 lors d'un second `dotnet run`.
Il a été arrêté après vérification de son chemin dans ce projet.

- `scripts/stop-backend.ps1` arrête uniquement les exécutables API de ce checkout.
- `scripts/start-backend.ps1` refuse un double lancement avant compilation ;
  `-Restart` permet d'arrêter l'instance locale avant de démarrer.
- Compilation Debug vérifiée : zéro erreur et zéro avertissement.
- Démarrage par le script vérifié avec `/health = ok`, puis instance de test arrêtée.
- Un second arrêt sans instance active réussit sans erreur.

Le backend est laissé arrêté pour permettre le lancement dans le terminal utilisateur.

## Résultats du 24 septembre 2026

- Backend : compilation Release réussie, 18 tests réussis, aucun ignoré.
- Les tests d'intégration utilisent PostgreSQL sur le port 55432 ; le test
  documentaire crée une base temporaire et un répertoire privé isolés,
  puis les supprime en fin de test.
- Frontend : ESLint, TypeScript et compilation de production réussis.
- Les pages `/conversions`, `/templates` et `/client-space`; l’ancienne URL `/archives` redirige vers l’espace client
  répondent HTTP 200 sur le frontend local. Le détail `/statements/[id]` figure
  dans le build et son API est couverte par les tests d'intégration.
- API locale : `/health` renvoie `ok`. Les deux migrations documentaires sont appliquées.
- EF Core ne détecte aucune modification de modèle en attente de migration.
- Après le redémarrage final, les empreintes des référentiels sont inchangées :
  2 utilisateurs, 3 clients, 9 banques et 1 compte, photos et logos compris.
- Le point de contrôle de la reprise précédente contenait 6 banques ; les autres
  catégories étaient identiques. Cette différence a été conservée sans suppression.
  Le contrôle final compare le contenu au début de cette reprise, avec 9 banques.

## Scénarios couverts

- Connexion, inscription, administration, droits et révocation des sessions.
- CRUD des référentiels, images et suppressions détaillées.
- Signature PDF invalide, document cassé et extraction de texte d'une fixture.
- Dépôt PDF authentifié, doublon sur le même compte, stockage privé.
- Refus de lecture de relevé, PDF et exports appartenant à un autre utilisateur.
- Analyse asynchrone sans profil : format inconnu, aucune transaction inventée.
- Profil synthétique : extraction automatique et normalisation du montant.
- Revue manuelle, conservation de l'identifiant et de la ligne source après correction.
- Refus des mises à jour avec une ancienne version et d'une revue après archivage.
- Dates, montants, soldes de début/fin et solde intermédiaire incohérent.
- Génération des trois types d'export, encodages, guillemets et protection des formules.
- Archivage, filtres par banque/période/export, téléchargement des exports conservés.
- Protection d'un compte bancaire utilisé par des relevés contre suppression
  et modification de son identité.

## Reproduire

Le rôle PostgreSQL de test doit pouvoir créer des bases. Ne jamais pointer les
commandes de test sur une base contenant des données utilisateur.

```powershell
$env:TEST_DATABASE_URL = 'Host=127.0.0.1;Port=55432;Database=bankconverter_tests;Username=converter'
dotnet test BankStatementConverter.sln -c Release
```

Dans `frontend/Import-pdf-excel-master` :

```powershell
npm run lint
npm run build
```

Contrôles API en lecture seule, avec les identifiants actuels d'un Admin :

```powershell
$env:SMOKE_EMAIL = 'adresse-administrateur'
$env:SMOKE_PASSWORD = 'mot-de-passe-actuel'
node scripts/smoke-catalogs.mjs
node scripts/smoke-statements.mjs
Remove-Item Env:SMOKE_PASSWORD
```

La tentative de contrôle authentifié sur la base principale a renvoyé
« Identifiants invalides » avec les identifiants du fichier local. Aucun mot de
passe n'a été réinitialisé. L'authentification et le parcours documentaire sont
validés séparément par les tests sur la base temporaire.

## Limites de la recette

Les tests emploient des documents synthétiques. Aucun profil bancaire réel ni
fichier importé avec succès dans une installation Sage n'a été validé.
Tesseract n'est pas disponible localement : l'exécution OCR réelle reste à tester.
Docker et le déploiement public ne sont pas testés dans cette session.
Aucun navigateur connecté n'était disponible pour une recette visuelle authentifiée.
Les réponses HTTP 200 ne vérifient pas les clics, le rendu mobile ou les deux thèmes.

Voir le [guide des relevés](statements.md) pour la configuration et les limites
fonctionnelles restantes. Les vérifications de build ne certifient pas la prise
en charge de tous les formats décrits dans le cahier des charges.
