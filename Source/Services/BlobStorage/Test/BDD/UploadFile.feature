Feature: As service's client I can upload files
    Scenario: I can upload a single file
        Given I post "../test_data/test_file.ext"
        When  I subscrube to "BlobUploaded" event
        Then  I should see "file.ext" on event body
