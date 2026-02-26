import asyncio
from api.api import generate_wiki_structure
class MockRequest:
    repo_url = "https://github.com/test/repo"
    messages = [{"role": "user", "content": "Return some XML"}]
    type = "github"

async def test():
    req = MockRequest()
    try:
        async for chunk in generate_wiki_structure(req):
            print(chunk)
    except Exception as e:
        print(f"Error: {e}")

asyncio.run(test())
