Feature: Public and administration experiences
  Administrators manage task ownership and review reports from a separate workspace.
  The repository and pipeline hold its functional design and test evidence.

  @FD-01 @Design
  Scenario: Visitors see the product without engineering documentation in the application
    Given I am visiting the public site
    When I open the home page and try the former functional design URL
    Then I see the public layout with a link to Tasks
    And the former functional design URL returns 404

  @FD-02 @Navigation
  Scenario: Administrators have their own navigation
    Given I am signed in as an administrator
    When I open the administration dashboard
    Then I see administrative navigation, a reports link and a link back to the site

  @FD-03 @Ownership
  Scenario: The owner picker lists all accounts and saves the selected owner
    Given user accounts span more than one page of admin search results
    When I edit an unassigned task
    Then I can filter the owner dropdown and select an account from the complete list
    And I can leave the task unassigned
    And an unknown account identifier is rejected

  @FD-04 @Filtering
  Scenario: Administrators clear the active filters
    Given I have searched the user list or filtered the task list
    When I follow Clear filters
    Then I return to the corresponding unfiltered list on page one

  @FD-05 @Reporting
  Scenario: Reports summarize actual task data
    Given there are open, completed and unassigned tasks
    When I open Reports
    Then I see task status and ownership counts from the database
    And a regular user is denied access

  @FD-06 @Verification
  Scenario: The pipeline publishes requirement results and optional test details
    Given a pull request triggers the test workflow
    When the tests finish and the report is generated
    Then the workflow summary shows an outcome for each requirement
    And I can expand test details or download the raw TRX report
    And a requirement without evidence cannot pass the coverage check
