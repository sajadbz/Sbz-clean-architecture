# Functional Design

This document defines the public and administrative behavior of Sbz Tasks and how it is verified. The [Given/When/Then scenarios](acceptance/design-and-admin.feature) use the same requirement IDs.

## FD-01 Public site
Visitors can open the public home page and navigate to their task list. The application keeps functional design and engineering reports in GitHub. The public navigation has no Functional Design link, and the former `/design` route returns 404.

## FD-02 Administration layout
Administrators use a separate responsive layout with navigation to Users, Tasks, and Reports, and a link back to the public site. The application restricts the administration workspace to users with the Administrator role.

## FD-03 Task ownership
The administration task form lists every account in a searchable owner dropdown. Filtering preserves the current selection. Administrators can leave a task unassigned, and the server rejects unknown owner IDs. Titles are unique per owner. A user can access tasks only when assigned as their owner.

## FD-04 Clear filters
On Users and Tasks, Clear filters removes the search query, clears the unassigned filter where applicable, and returns to page one.

## FD-05 Dashboard and reports
The dashboard summarizes account and task counts. Reports show total, open, completed, and unassigned task counts, and the ten owners with the most assigned tasks. The totals come from the SQLite database. Only administrators can access the report.

## FD-06 Verification on GitHub
Pull requests and pushes to `master` run the automated tests. The GitHub Actions run summary shows a status for each requirement and expandable test details with category, duration, and timestamps when available. The `test-results` artifact contains the raw TRX. CI fails if a declared requirement lacks test evidence.

## Updating the design

Use the same `FD-NN` identifier in this document, the Gherkin tag, and the acceptance test display name. The .NET tests execute the web and database scenarios, the Node test verifies the owner search interaction, and the report test verifies the CI summary. Gherkin is the readable specification for those tests.
