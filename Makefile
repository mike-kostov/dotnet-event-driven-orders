# Makefile — short names for the long docker compose commands.
# Run `make <target>`, e.g. `make up`. This file is plain text you can read and
# extend — it is NOT magic. (In step 5 you add your own target.)
#
# Recipe lines MUST be indented with a TAB, not spaces — that's a Make rule.

.PHONY: up down logs ps psql topics consume lag migrate

up:        ## Build images and start all containers in the background
	docker compose up --build -d

down:      ## Stop containers AND delete their data volumes (a clean reset)
	docker compose down -v

logs:      ## Follow the logs from every container (Ctrl-C to stop watching)
	docker compose logs -f

ps:        ## Show the status of each container
	docker compose ps

psql:      ## Open a psql shell in the postgres container (solution to lesson 1, step 5)
	docker compose exec postgres psql -U orders -d orders

topics:    ## List Kafka topics and their partition counts (lesson 3)
	docker compose exec kafka /opt/kafka/bin/kafka-topics.sh --bootstrap-server kafka:9092 --describe

consume:   ## Tail the orders topic from the beginning, showing keys (Ctrl-C to stop) (lesson 3)
	docker compose exec kafka /opt/kafka/bin/kafka-console-consumer.sh --bootstrap-server kafka:9092 --topic orders --from-beginning --property print.key=true

lag:       ## Show consumer-group lag for order-processor (lesson 4)
	docker compose exec kafka /opt/kafka/bin/kafka-consumer-groups.sh --bootstrap-server kafka:9092 --describe --group order-processor

migrate:   ## Re-run database migrations (DbUp; applies any new .sql) (lesson 5)
	docker compose up --build migrate
