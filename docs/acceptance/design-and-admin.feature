Feature: Separate public and administration experiences
  The website publishes its functional design in English.
  Administrators manage task ownership and review reports from a separate workspace.

  @FD01 @Design
  Scenario: Visitors can identify the public site and read its functional design
    Given I am visiting the public site
    When I open the home page and the functional design page
    Then I see the public layout and the English requirements

  @FD02 @Navigation
  Scenario: Administrators have their own navigation
    Given I am signed in as an administrator
    When I open the administration dashboard
    Then I see administrative navigation, a reports link and a link back to the site

  @FD03 @Ownership
  Scenario: The owner picker lists all accounts and saves the selected owner
    Given user accounts span more than one page of admin search results
    When I edit an unassigned task
    Then I can filter the owner dropdown and select an account from the complete list
    And I can leave the task unassigned
    And an unknown account identifier is rejected

  @FD04 @Filtering
  Scenario: Administrators clear the active filters
    Given I have searched the user list or filtered the task list
    When I follow Clear filters
    Then I return to the corresponding unfiltered list on page one

  @FD05 @Reporting
  Scenario: Reports summarize actual task data
    Given there are open, completed and unassigned tasks
    When I open Reports
    Then I see task status and ownership counts from the database
    And a regular user is denied access

  @FD06 @Verification
  Scenario: Test results can be inspected on GitHub
    Given a pull request triggers the test workflow
    When the tests finish
    Then the workflow summary shows their outcomes
    And I can expand test details or download the raw TRX report
