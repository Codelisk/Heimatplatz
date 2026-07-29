# Heimatplatz

Die gemeinsamen Projektregeln stehen in [`CLAUDE.md`](CLAUDE.md) und sind fuer Codex
vollstaendig verbindlich. Lies diese Datei vor jeder Aenderung am Repository und behandle
ihre Vorgaben so, als stuenden sie direkt in diesem `AGENTS.md`.

## Agent-spezifische Integration

- Codex-Projektkonfiguration: `.codex/config.toml`
- Codex-Skills: `.codex/skills/`
- Claude-Projektkonfiguration: `.claude/settings.json`
- Claude-Skills: `.claude/skills/`
- Fachliche Regeln und Architekturvorgaben werden nur in `CLAUDE.md` gepflegt, damit
  Claude und Codex denselben Projektkontext verwenden.
- Inhaltlich gemeinsame Skills muessen in beiden Skill-Verzeichnissen vorhanden sein.
  Claude-spezifische Frontmatter-Felder (`auto_invoke`, `triggers`) werden nicht in
  Codex-Skills uebernommen; bei Codex gehoeren Ausloeser in die `description`.

## Skills

Waehle bei jeder Aufgabe die kleinste passende Skill-Menge:

- Web/Astro: `astro-ai-development`
- Vollstaendige manuelle QA-Laeufe fuer Web oder MAUI: `funktionstest`
- Lokalisierung mit `Shiny.Extensions.Localization.Generator`: `localizegen`
- MAUI und Shiny: den oder die fachlich passenden `shiny-*` Skills

Lies die jeweilige `SKILL.md` vollstaendig, bevor du fachliche Aenderungen beginnst,
und lade referenzierte Dateien nur bei Bedarf nach.
