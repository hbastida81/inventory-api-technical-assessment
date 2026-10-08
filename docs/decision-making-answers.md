# Decision-Making Questions

These answers were authored by the human developer as part of the technical assessment.

---

## 1. Describe a recent technical decision you made with incomplete information. What information was missing and how did you decide to move forward?

A recent decision was choosing how to implement OAuth2 in a technical assessment where the requirement asked for OAuth2 authentication but did not specify a provider or how the authorization server should be deployed. I was missing information about what solution the evaluator expected and whether it was appropriate to depend on an external service. I decided to move forward by prioritizing a self-contained and reproducible solution, which is why I chose Keycloak. This allowed me to include the realm, the OAuth client, the evaluation user, and the complete authentication flow inside Docker, without depending on any external configuration.

## 2. When two technical approaches seem equally valid, what criteria do you use to choose one?

When I have two similar options, I go with the one that is simpler, easier to maintain, and does not overcomplicate the project. I also consider security and how easy it will be to test. If I am still unsure, I try a small proof of concept before committing to a decision.

## 3. Describe a situation where you had to reverse or change one of your own technical decisions. What did you learn?

For inventory exits, I initially planned to read the stock and then update it, but then I realized that if two operations came in at the same time there could be a problem. So I changed it so the validation and update happen in the same operation and inside a transaction. I learned that you always have to think about what can happen when multiple operations are running at the same time.
