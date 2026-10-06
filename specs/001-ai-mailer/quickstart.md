# Quickstart validation

From repository root run `bash container/run-local.sh` and open
http://localhost:8088. Requires .NET 10 and Node 22+.

The first local visitor is demo@alza.cz and is administrator. Local mode uses
memory (resets at restart), static drafting, sample rows, and simulated delivery.
No real email is sent and no Google resource is read.

Run `dotnet test container/backend.tests` and
`npm --prefix container/frontend run build`. Verify compose -> generate -> edit
-> review -> simulate, access administration, final-admin protection, test-data
preview, mobile layout, and unauthorized identity via HTTP tests.

Cloud validation follows docs/deployment.md after a dedicated Vibe project,
webhook credentials/mailbox, configured Vertex model, and academy table exist.
