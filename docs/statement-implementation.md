# Reconstruction des relevés et archives

## Analyse préalable

Architecture actuelle : Domain (User, Client, Bank, BankAccount), Application
(contrats), Infrastructure (catalogues, identité, médias, EF Core), API .NET 8.
Frontend Next.js : dashboard, clients, banks, bank-accounts, users, settings,
login et register. Authentification JWT, permissions actualisées en base.
L'ancien workflow PDF n'est plus actif. Les anciennes tables ont été retirées
par RemoveDocumentProcessing ; les migrations historiques doivent rester.
Aucun PDF bancaire représentatif ni parser bancaire actif n'a été trouvé.

| Fichier / groupe | Action | Raison |
|---|---|---|
| Domain/Entities.cs et catalogues | Conserver | Référentiels opérationnels |
| Identity et contrôleurs Auth/Users | Conserver, étendre permissions | Non-régression |
| Persistence/AppDbContext.cs | Étendre | Nouveau modèle de relevés |
| Migrations historiques | Conserver | Traçabilité et mises à niveau |
| Domain/Statements.cs | Créer | Relevés, opérations, profils, exports, historique |
| Application/StatementContracts.cs | Créer | Contrats indépendants des transports |
| Infrastructure/Statements | Créer | Services spécialisés |
| API/Controllers/Statements* | Créer | Routes protégées |
| Frontend conversions/templates/archives/client-space | Créer | Parcours utilisateur |

## Cible et relations

Client -> BankAccount -> BankStatement -> BankTransaction.
Bank -> BankStatementProfile. BankStatement -> ProcessingHistory et StatementExport.
ExportTemplate -> ExportTemplateField. Les exports consomment uniquement les
opérations normalisées et validées, jamais le PDF. Téléchargement sans retraitement.
Stockage privé, clés opaques, originaux conservés, contraintes restrictives sur
la suppression des référentiels liés. Modèles Sage configurables, sans garantie
universelle de compatibilité. Pas de création de clients, banques ou comptes fictifs.

## Phases

1. Modèle et migration additive.
2. Stockage, analyse PDF, extraction texte/OCR et traitement asynchrone.
3. Revue, normalisation, validation et historique.
4. Modèles, génération et conservation des exports.
5. API avec contrôle de permission et propriété des documents.
6. Écrans, navigation, espace client et archives.
7. Tests unitaires/intégration, compilation et documentation de lancement.

## Risques et vérifications

Les profils réels nécessitent des PDF représentatifs anonymisés. Un format inconnu
ne doit pas produire de transactions inventées. OCR nécessite des exécutables
locaux configurés. Tester les doublons, fichiers invalides/protégés, soldes,
formats numériques/dates, exports et encodages, téléchargements protégés,
concurrence, reprise et absence de régression des catalogues.
