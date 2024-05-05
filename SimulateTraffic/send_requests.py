import argparse
import aiohttp
import asyncio
from tqdm import tqdm  # Use regular tqdm

async def send_requests(url, requests_per_second, duration_seconds):
    # Warm-up request before starting the main loop
    async with aiohttp.ClientSession() as session:
        print("Sending initial request...")
        await send_request(session, url)  # Send a single warm-up request
        print("Initial request sent. Waiting 5 seconds before starting the main loop...")
        await asyncio.sleep(5)  # Wait for 5 seconds after the initial request

        total_requests = requests_per_second * duration_seconds
        interval = 1.0 / requests_per_second

        tasks = []
        progress = tqdm(total=total_requests, desc="Sending Requests", unit="req")

        for _ in range(total_requests):
            task = asyncio.ensure_future(send_request(session, url, progress))
            tasks.append(task)
            await asyncio.sleep(interval)  # Sleep asynchronously

        responses = await asyncio.gather(*tasks)
        progress.close()

        # Analyze responses (just count them in this example)
        status_counts = {status: responses.count(status) for status in set(responses) if status is not None}
        print(f"Finished sending requests. Status counts: {status_counts}")

async def send_request(session, url, progress=None):
    try:
        async with session.get(url) as response:
            if progress:
                progress.update(1)
            return response.status
    except Exception as e:
        if progress:
            progress.write(f"An error occurred: {e}")
        return None

def main():
    parser = argparse.ArgumentParser(description='Send HTTP requests at a specified rate to a given URL.')
    parser.add_argument('url', type=str, help='The URL to send requests to')
    parser.add_argument('requests_per_second', type=int, help='Number of requests per second')
    parser.add_argument('duration_seconds', type=int, help='Duration to send requests in seconds')

    args = parser.parse_args()

    asyncio.run(send_requests(args.url, args.requests_per_second, args.duration_seconds))

if __name__ == '__main__':
    main()
