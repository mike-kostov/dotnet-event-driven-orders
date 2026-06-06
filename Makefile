# Makefile — short names for the long docker compose commands.
# Run `make <target>`, e.g. `make up`. This file is plain text you can read and
# extend — it is NOT magic. (In step 5 you add your own target.)
#
# Recipe lines MUST be indented with a TAB, not spaces — that's a Make rule.

.PHONY: up down logs ps psql

up:        ## Build images and start all containers in the background
	docker compose up --build -d

down:      ## Stop containers AND delete their data volumes (a clean reset)
	docker compose down -v

logs:      ## Follow the logs from every container (Ctrl-C to stop watching)
	docker compose logs -f

ps:        ## Show the status of each container
	docker compose ps

# TODO(you) step 5 — add a `psql` target that opens a SQL shell inside the
#   postgres container, so you never have to remember the long command.
#   See LESSON.md step 5.
#   hint: docker compose exec postgres psql -U orders -d orders
#
# psql:    ## Open a psql shell in the postgres container
# 	<your command here>
