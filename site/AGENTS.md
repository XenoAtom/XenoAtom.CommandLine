---
discard: true # Disable this page for website generation
---

# Website (lunet) Contribution Instructions

This folder contains the static website for XenoAtom.CommandLine, built with **lunet**.

## Structure

- `site/readme.md` -> home page (`/`)
- `site/docs/**` -> documentation section (`/docs/**`)
  - `site/docs/menu.yml` -> sidebar menu for docs pages
- `site/menu.yml` -> top navigation (navbar)
- `site/.lunet/**` -> layouts, CSS, JS, and build output
  - `site/.lunet/layouts/**` -> Scriban HTML layouts
  - `site/.lunet/css/**` / `site/.lunet/js/**` -> site assets
  - `site/.lunet/build/**` -> generated output (cache + `www/`)

## Building the website

Install lunet (once):

`dotnet tool install -g lunet`

Build the site (always run this after changing `site/**`):

`lunet build`

Run from the `site` folder.

## Notes for agents

- `readme.md` in a folder becomes the folder's `index.html`. Keep these `readme.md` files compatible with both GitHub and the website.
- Update menus when adding/moving pages:
  - `site/menu.yml` (top nav)
  - `site/docs/menu.yml` (docs sidebar)
- Specs under `site/docs/specs/**` are internal notes and should stay excluded from website navigation/public output.
