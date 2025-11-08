@ui
Feature: Remove User
    As a room admin
    I want to manage participants in my Secret Nick room
    So that I can organize and control the gift exchange process

Background:
    Given the API is available
    And I am on the home page

Rule: Participant removal by admin

  @positive
  Scenario Outline: Remove participant before randomization started
    Given a room exists with 2 participant via API
    When I navigate to room page with admin code
    And I click "Remove user" icon
    And I click "Remove" button
    Then I should see participants count 1

  @negative
  Scenario: Try to remove participant after randomization started
    Given a room exists with 3 participants via API
    When I navigate to room page with admin code
    And I click "Draw Names" button
    And I click "Remove user" icon
    And I click "Remove" button
    Then I should see participants count 3