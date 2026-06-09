# Lesson 01 — Tooling & dev environment

> **Where you are:** the very beginning. You don't need to know any backend yet.
> By the end you'll have run a real Kafka + Postgres stack with one command, and
> packaged your own program into a container.
>
> **How lessons work** (see the repo's branch model): you're on the branch
> `lesson/01-tooling`. It already has everything *placed* for you — you just fill
> in the spots marked `TODO(you)`, following the steps below. When you finish,
> compare your work with the next branch, `lesson/02-order-ingest-api`.

---

## 1. Why this lesson exists

A backend system is several programs that must run **the same way** on every
machine — yours, a teammate's, the cloud. Before we write any of those programs,
we need a reliable way to *run things*. That's what this lesson is about.

You'll meet three tools you'll use in every later lesson:

- **Docker** — runs software in **containers**: isolated, reproducible little boxes.
- **Docker Compose** — starts a *set* of containers together from one file.
- **Make** — remembers long commands for you, so you type `make up` instead.

We bring up the real infrastructure (Kafka + Postgres) now so these tools are
learned against the actual moving parts, not a toy.

---

## 2. Concepts (read once, it'll click as you go)

- **Dockerfile** — a recipe. A list of steps to build an image.
- **Image** — the result of that recipe: a frozen snapshot of your app + the
  exact environment it needs. Same image → same behaviour everywhere.
- **Container** — a *running* instance of an image. You can start/stop many
  containers from one image. When the program inside finishes, the container stops.
- **Compose** — `docker-compose.yml` lists several services (containers) and how
  they relate (e.g. "start `hello` only after `postgres` is healthy").
- **Make** — `make up` runs whatever the `up:` recipe in the `Makefile` says.

> Why Compose and not something fancier (like .NET Aspire)? We want the moving
> parts *visible* while you learn them — see `docs/adr/0013-compose-only-no-aspire-apphost.md`.

---

## 3. Do this first — bring up the infrastructure

You need **Docker** and **make** installed. Check:

```bash
docker --version
docker compose version
make --version
```

> **Using Podman instead of Docker?** (e.g. your company standardizes on it.)
> Podman is a drop-in replacement, so this whole tutorial works unchanged — you
> just point the `docker` commands at Podman once, here in lesson 1:
> - **Simplest:** alias `docker` to `podman`. Podman ships a Docker-compatible CLI
>   and `podman compose`, so every `make` target and `docker compose …` command in
>   these lessons runs as-is. (On Podman Desktop, enabling the Docker-compatible
>   socket achieves the same thing.)
> - **One caveat:** this tutorial relies on Compose health-gating
>   (`depends_on: condition: service_healthy` / `service_completed_successfully`).
>   Use a recent **`podman compose`** (Podman 4.7+, which uses Compose v2) — the
>   older Python `podman-compose` may not honor those conditions.
>
> Everything below (and in every later lesson) is then identical; we say "Docker"
> throughout, but it's really "your container engine."

Create your local env file (it's gitignored), then start just the infra:

```bash
cp .env.example .env
docker compose up -d postgres kafka
docker compose ps
```

Wait until both show **healthy** (re-run `docker compose ps` a few times). You
just started a Kafka broker and a Postgres database with two commands. 🎈

> **Hit `Error ... port is already allocated`?** Something else on your machine
> is already using port 5432 (another Postgres) or 9092 (another Kafka). You don't
> have to hunt it down — just open `.env` and change `POSTGRES_PORT` (e.g. to
> `5434`) or `KAFKA_PORT`, then `make down` and `make up` again. This is exactly
> why those ports live in `.env`.

> `hello` won't start yet — it has no Dockerfile. That's step 4.

---

## 4. Build the `hello` Dockerfile  ← your main task

Open `hello/Dockerfile`. There's a tiny .NET program in `hello/Program.cs`
already (read it — it just prints a greeting). Your job: write the recipe to
package it. Replace each `TODO(you)` with one instruction:

- **4.1 — `FROM`**: start from the .NET 10 SDK image.
  `FROM mcr.microsoft.com/dotnet/sdk:10.0`
- **4.2 — `WORKDIR`**: set the working dir inside the image.
  `WORKDIR /app`
- **4.3 — `COPY` + `RUN`**: copy the source in and publish a build.
  `COPY . .` then `RUN dotnet publish -c Release -o /out`
- **4.4 — `ENTRYPOINT`**: run the published app.
  `ENTRYPOINT ["dotnet", "/out/hello.dll"]`

Now build and run everything:

```bash
make up                       # builds the hello image, starts all three
docker compose logs hello     # see your program's output
```

You should see the 👋 greeting. You just built an image from a Dockerfile and
ran it as a container. The `hello` container then exits (its job is done) — that
is normal; `docker compose ps -a` will show it as `Exited (0)`.

---

## 5. Your turn — add a `make psql` shortcut

Opening a database shell is something you'll do constantly. Typing the full
command every time is tedious — that's exactly what Make is for.

Open the `Makefile`, find the `TODO(you) step 5` block, and add a `psql` target:

```make
psql:      ## Open a psql shell in the postgres container
	docker compose exec postgres psql -U orders -d orders
```

> The recipe line **must start with a TAB**, not spaces.

Then:

```bash
make psql
# at the psql prompt, type:  \dt    (lists tables — none yet, that's fine)
# quit with:  \q
```

---

## 6. You're done when

- [ ] `docker compose ps` shows `kafka` and `postgres` **healthy**.
- [ ] `make up` builds `hello` and its log shows the 👋 greeting.
- [ ] `make psql` drops you into a Postgres shell.
- [ ] You can say, in your own words, what a Dockerfile, an image, and a
      container are, and what `make up` does.
- [ ] Clean reset works: `make down` then `make up` brings it all back.

Compare your result with the next branch to check your work:

```bash
git diff lesson/02-order-ingest-api -- hello/Dockerfile Makefile
```

---

## 7. Next

In **lesson 02** you write your first real service: `order-ingest`, an HTTP API
that accepts orders. Check out `lesson/02-order-ingest-api` and open its
`LESSON.md`.
