// A tiny program whose only job is to prove we can package OUR OWN code into an
// image and run it as a container. In lesson 2 we replace this with a real
// HTTP service (order-ingest). For now: containerize, run, see output. Done.

Console.WriteLine("👋  Hello from inside a container!");
Console.WriteLine($"    Running as host: {Environment.MachineName}");
Console.WriteLine("    Compose waited for Kafka + Postgres to be healthy before starting me.");
Console.WriteLine("    You just built an image from a Dockerfile and ran it. That's the lesson. 🎉");
