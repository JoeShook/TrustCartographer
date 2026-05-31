---
name: feedback-no-background-apphost
description: "Do not leave background AppHost instances running; they collide with the user's own run"
metadata: 
  node_type: memory
  type: feedback
  originSessionId: 6b1ccb6a-277f-4760-92dd-c0f12f5d5930
---

When verifying changes, do NOT leave a background Aspire AppHost running after the turn, and do not start one while the user may be running their own.

**Why:** the AppHost binds fixed ports (7257 web, 17051 dashboard) and the dashboard mints a new login token each launch. A leftover verification instance collides with the user's own `dotnet run`, so the user's dashboard token is rejected (their AppHost couldn't bind). This caused repeated "login is failing" frustration across the session.

**How to apply:** prefer `dotnet build` for verification; run targeted curl checks only against an instance the user is already running, or if you must start the AppHost, kill it (tree + the detached dashboard child) in the **same turn** before replying. Never hand control back with a verification AppHost alive. See [[reference-running-the-app]].
