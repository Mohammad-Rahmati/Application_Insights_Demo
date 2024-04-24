import time
from locust import HttpUser, task, between, events
from locust.env import Environment
from locust.stats import stats_printer
from locust.log import setup_logging

setup_logging("INFO", None)  # Set up logging

class WebsiteUser(HttpUser):
    host = "https://app-insights-demo-net.azurewebsites.net"
    wait_time = between(1, 5)  # Users wait between 1 to 5 seconds between tasks

    @task
    def get_api_simulation(self):
        self.client.get("/ApiSimulation")

# Setup Environment and Runner
env = Environment(user_classes=[WebsiteUser])
env.create_local_runner()

# Optional: Start a WebUI instance
env.create_web_ui("127.0.0.1", 8089)

# Function to print stats
def print_stats():
    print(env.runner.stats.total.summary())
    print(env.runner.stats.total.stats())

# Event hook to print stats at interval
@events.test_stop.add_listener
def on_test_stop(environment, **kwargs):
    print_stats()

# Start the test
env.runner.start(user_count=10, spawn_rate=2)  # Modify these numbers based on your load requirement

# In 60 seconds stop the runner
time_limit = 60*20
time.sleep(time_limit)
env.runner.quit()

# Stop the web server that provides the web UI
env.web_ui.stop()
