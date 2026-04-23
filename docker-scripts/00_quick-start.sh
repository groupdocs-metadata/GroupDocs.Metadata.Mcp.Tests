#!/bin/bash
################################################################################
# QUICK START REFERENCE — CHEATSHEET
#
# This file is a copy-paste reference, NOT a script to execute. Running it
# would run the whole suite several times with different filters. Read it.
################################################################################

cat <<'USAGE'
This is a cheatsheet. Open it in your editor and copy commands as needed.
See README.md for details.
USAGE
exit 0


# ===== ONE-TIME SETUP =====
cd docker-scripts
chmod +x *.sh          # Make scripts executable (Linux/macOS only)
./01_verify-setup.sh   # Verify environment is ready


# ===== MOST COMMON COMMANDS =====

# Run all 12 tests locally (FASTEST)
./02_test-all-scenarios.sh

# Run tests in Docker containers
./03_test-docker-compose.sh

# Run only ToolDiscovery tests (2 seconds, validates server startup)
./02_test-all-scenarios.sh --filter ToolDiscovery

# Run only ReadMetadata tests
./02_test-all-scenarios.sh --filter ReadMetadata

# Test specific version
./02_test-all-scenarios.sh --version 26.4.4

# Use GroupDocs license (unlocks RemoveMetadata tests)
./02_test-all-scenarios.sh --license /path/to/GroupDocs.Total.lic


# ===== DOCKER-SPECIFIC COMMANDS =====

# Run in Docker with custom version
./03_test-docker-compose.sh --version 26.4.4

# Run in Docker and keep containers (for debugging)
./03_test-docker-compose.sh --keep

# Manual cleanup (if containers still running)
docker compose -f docker-compose.test.yml down -v


# ===== ADVANCED USAGE =====

# Test multiple versions sequentially
for v in 26.4.3 26.4.4 26.5.0; do
  ./02_test-all-scenarios.sh --version $v || exit 1
done

# Test with environment variable (alternative to --license)
export GROUPDOCS_LICENSE_PATH=/absolute/path/to/license.lic
./02_test-all-scenarios.sh

# Run with verbose output
export DEBUG=1
./02_test-all-scenarios.sh


# ===== TROUBLESHOOTING =====

# Verify Docker is running
docker ps

# View Docker logs
docker compose -f docker-compose.test.yml logs -f test-runner

# Check available Docker images
docker images | grep -E "dotnet|groupdocs"

# Clean up all Docker resources
docker system prune -a


# ===== EXPECTED OUTPUT =====
# ✓ All integration test scenarios completed!
# Total: 12, Passed: 12, Time: ~13s
