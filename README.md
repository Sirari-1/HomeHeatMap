# HomeHeatMap

Blazor Server app for exploring Florida city crime data with map and table views.

## Deploy to Render

This repo includes `Dockerfile` and `render.yaml` for one-click Render deployment.

- Push latest changes to `master`.
- In Render, create a new Web Service.
- Connect GitHub repo: `https://github.com/Sirari-1/HomeHeatMap`
- Render will detect `render.yaml` automatically.
- Create the service and wait for the first deploy.
- Share the generated public URL (example: `https://homeheatmap.onrender.com`).

## Local run

- From `HomeHeatMap` project folder:
- `dotnet restore`
- `dotnet run`
- Open the local URL shown in terminal.
