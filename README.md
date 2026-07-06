# Groupe 4 - LED

Projet de spectacle son et lumière synchronisé (ArtNet/DMX) - Unity 6 (6000.4.9f1), pipeline URP.

## Structure

- `Assets/` - contenu du projet Unity (scripts, scènes, prefabs...)
- `Packages/` - dépendances du projet (manifest.json)
- `ProjectSettings/` - configuration du projet (graphics, quality, input...)

Les dossiers `Library/`, `Temp/`, `Logs/`, `UserSettings/` sont générés localement par Unity et ne sont pas versionnés (voir `.gitignore`).

## Convention de nommage des branches

Chaque branche correspond à un ticket Trello, au format :

```
feature/<id-ticket-en-minuscule>-<description-courte>
```

Exemples :
- `feature/infra-01-init-unity-project`
- `feature/infra-02-artnet-sender`
- `feature/infra-04-led-simulator`
- `feature/infra-03-render-to-texture`

Pour un correctif : `fix/<id-ticket>-<description>`.
