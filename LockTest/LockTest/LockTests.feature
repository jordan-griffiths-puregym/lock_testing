Feature: SFTP lock tests

Scenario: No lock file already on remote
	Given there is no lock file
	When a request is made to generate a new recon file and add it to the SFTP box
	Then a new lock file is wrote
	And the SFTP box includes the most recently generated recon file
	And no lock files exist

Scenario: One or more stale lock files already on remote, and no active lock
	Given there are stale lock files
	When a request is made to generate a new recon file and add it to the SFTP box
	Then the stale lock files have been deleted
	And a new lock file is wrote
	And the SFTP box includes the most recently generated recon file
	And no lock files exist

Scenario: One or more stale lock files already on remote, and one active lock
	Given there is an active lock file
	When a request is made to generate a new recon file and add it to the SFTP box
	Then a HyveFileLockedException exception is thrown
	And the active lock file still exists
	And no stale lock files exist