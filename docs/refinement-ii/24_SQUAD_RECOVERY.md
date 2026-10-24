# Squad recovery — issue #24 design

Baseline a37b48f (#23), immediate rewards1, three actual preparations, wire19 /
battleflow4. #23 local gates all pass; its exact-SHA CI81 remains running.
Preserve baseline source/captures and every compared failure. No Core change is
selected by this design.

Build a runnable, bounded wrapper over the existing Store/Return member commands.
Select a wounded squad/home, review exact IDs, current wounds, chosen hall and
space, next actual eligible healing, future stored Food and field absence; commit
once. Only members that fit are promised. Healthy members stay in the field.
Return recovered members is another explicit finite review/commit, using Core's
current compatible destination projection. Never retire/recruit automatically.

This sequence is **not atomic**. Submit at most one ordinary command at a time;
wait for its actual acknowledgment AND accepted assignment snapshot before the
next. A duplicate ACK must not advance twice. Cancel stops unsent operations;
an already accepted/in-flight move remains real and must be reported. Reconnect
or session replacement cancels the local sequence; a fresh preview excludes
already moved members. No persistent automation or batch state in preferences.

Freeze identity, phase/turn, roster health/profile/assignments, hall generation/
capacity/healing, plots, funds and next income. Recheck before each submission;
only the reviewed sequence's acknowledged assignment changes may differ. Pause,
Ready, selection/city/session/phase changes or unrelated owned changes stop/requote.
Allied/fallen/terminal views have no executable plan. Retirement keeps its existing
separate reviewed irreversible flow and safe cancel.

Keep existing Core Food priority, eligibility, individual capacity, healing tracks,
fixed equipment and service semantics. Store moves/heals no HP immediately.
Eligible storage heals once per actual Ready (three per ordinary wave), capped
by Core; stored members require funded Food and never fight. Ineligible members
may be stored but cannot promise healing until a completed funded battle. Immediate
recovery rewards and research/maximum-HP changes remain separate and invalidate
stale quotes. No free heal, resurrection, gear/XP benefit or refund.

Compare early healthy/same-tier and wounded cases, obsolete equipment, late full
field, Food shortage, full/partial/no hall, blocked returns and actual reward heal.
Record exact prices, next/cumulative eligible HP, time absent, Food, capacity,
participants and actions. De-emphasize recovery for dominated obsolete equipment;
Keep remains no action. Evaluate honest individual-versus-wrapper interaction
cost, including partial acceptance, cancellation and rejoin. Do not infer universal
optimality or human enjoyment from scripted cases.

Integrate one wounded-squad exception in the existing army context. Keep detailed
member exceptions/history optional; remove service counts from primary combat
comparison. No new maintenance dashboard or healthy-member checklist. Native
pointer/keyboard/controller events at720/1080 must verify real accepted counts,
healing, no refunds, stale-intent safety and the plain/art fallback. Required
build/Core/Shared, session/gameplay, campaign, multiplayer/reconnect and final CI
remain. A larger text comparison alone does not complete this issue.
