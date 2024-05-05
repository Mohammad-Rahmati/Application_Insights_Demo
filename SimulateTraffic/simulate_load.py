import time
import sys
from locust import HttpUser, task, between, events
from locust.env import Environment
from locust.stats import stats_printer
from locust.log import setup_logging

def initialize_logging():
    # Set up logging
    setup_logging("INFO", None)

class WebsiteUser(HttpUser):
    wait_time = between(1, 5)  # Users wait between 1 to 5 seconds between tasks

    @task
    def get_api_simulation(self):
        self.client.get("/")

def print_stats(environment):
    print(environment.runner.stats.total.summary())
    print(environment.runner.stats.total.stats())

def main():
    initialize_logging()

    # Check command line arguments
    if len(sys.argv) < 3:
        print("Usage: python script_name.py <host> <load_type>")
        sys.exit()

    host_url = sys.argv[1]
    load_type = sys.argv[2]

    # Initialize the user class with host
    WebsiteUser.host = host_url

    # Setup Environment and Runner
    env = Environment(user_classes=[WebsiteUser])
    env.create_local_runner()

    # Optional: Start a WebUI instance
    env.create_web_ui("127.0.0.1", 8089)

    # Event hook to print stats at interval
    @events.test_stop.add_listener
    def on_test_stop(environment, **kwargs):
        print_stats(environment)

    # Define load parameters based on input
    if load_type == "normal":
        user_count = 10
        spawn_rate = 2
    elif load_type == "heavy":
        user_count = 1000
        spawn_rate = 200
    else:
        print("Invalid load type. Please choose 'normal' or 'heavy'.")
        sys.exit()

    # Start the test
    env.runner.start(user_count=user_count, spawn_rate=spawn_rate)

    # In 60 seconds stop the runner
    time_limit = 60*10  # 10 minutes
    time.sleep(time_limit)
    env.runner.quit()

    # Stop the web server that provides the web UI
    env.web_ui.stop()

if __name__ == "__main__":
    main()
