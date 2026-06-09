# Makefile — short names for the long docker compose commands.
# Run `make <target>`, e.g. `make up`. This file is plain text you can read and
# extend — it is NOT magic. (In step 5 you add your own target.)
#
# Recipe lines MUST be indented with a TAB, not spaces — that's a Make rule.

.PHONY: up down logs ps psql topics consume lag

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

# --- lesson navigation (prev / next / solution / goto) ---
.PHONY: next prev solution goto
LESSON_SEQ := main lesson/01-tooling lesson/02-order-ingest-api lesson/03-kafka-producer lesson/04-kafka-consumer lesson/05-persistence-cqrs lesson/06-state-machine-transitions lesson/07-order-query lesson/08-testing lesson/09-reliability-dlq-replay lesson/10-observability-polish final

next:      ## Check out the NEXT lesson (commit or stash your edits first)
	@cur=$$(git rev-parse --abbrev-ref HEAD); f=0; n=""; for x in $(LESSON_SEQ); do if [ "$$f" = 1 ]; then n=$$x; break; fi; if [ "$$x" = "$$cur" ]; then f=1; fi; done; if [ -n "$$n" ]; then echo "-> $$n"; git checkout "$$n"; else echo "You are at the end (final)."; fi

prev:      ## Check out the PREVIOUS lesson
	@cur=$$(git rev-parse --abbrev-ref HEAD); p=""; for x in $(LESSON_SEQ); do if [ "$$x" = "$$cur" ]; then break; fi; p=$$x; done; if [ -n "$$p" ]; then echo "<- $$p"; git checkout "$$p"; else echo "You are at the start (main)."; fi

solution:  ## Show the diff from your work to the next lesson (the solution)
	@cur=$$(git rev-parse --abbrev-ref HEAD); f=0; n=""; for x in $(LESSON_SEQ); do if [ "$$f" = 1 ]; then n=$$x; break; fi; if [ "$$x" = "$$cur" ]; then f=1; fi; done; if [ -n "$$n" ]; then git --no-pager diff "$$n"; else echo "No next lesson to compare against."; fi

goto:      ## Check out a lesson by number, e.g. make goto LESSON=6
	@for x in $(LESSON_SEQ); do case "$$x" in lesson/0$(LESSON)-*|lesson/$(LESSON)-*) echo "-> $$x"; git checkout "$$x"; exit 0;; esac; done; echo "Usage: make goto LESSON=<1..10>"
