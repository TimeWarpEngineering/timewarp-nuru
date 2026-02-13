# Add HttpClient sample calling public API

## Description

Create a runnable sample demonstrating HttpClient usage in a Nuru CLI application. The sample should call a free public API and display the results. This fills a gap in our samples collection - we currently have no examples showing HTTP requests in actual runnable code.

## Checklist

- [ ] Research and select a suitable public API (no auth required, stable, interesting data)
- [ ] Create the sample in `/samples/fluent/13-httpclient/` directory
- [ ] Show both sync and async patterns if applicable
- [ ] Add proper error handling for HTTP failures
- [ ] Add README.md explaining how to run the sample
- [ ] Test the sample locally to ensure it works
- [ ] Update `/samples/fluent/readme.md` to include the new sample

## Notes

**Potential Public APIs to consider:**

1. **Open-Meteo Weather API** (https://open-meteo.com/) - Free, no API key, good for demonstrating simple GET requests with query parameters
2. **Random User API** (https://randomuser.me/) - Generates random user data, good for demonstrating JSON deserialization
3. **PokéAPI** (https://pokeapi.co/) - Pokemon data, fun and well-known
4. **REST Countries** (https://restcountries.com/) - Country information, good for search/filter patterns
5. **Cat Facts API** (https://catfact.ninja/) - Simple, no auth
6. **GitHub Public API** (limited endpoints) - Could show repo info or user data
7. **SWAPI (Star Wars API)** (https://swapi.dev/) - Good for demonstrating nested resource fetching

**Recommended approach:**
- Use Open-Meteo for weather by city name (people understand weather)
- Show route like: `weather {city}` or `weather --city {city} --days {days:int?}`
- Demonstrate JSON parsing with System.Text.Json
- Include proper timeout and error handling
- Consider showing both inline HttpClient creation and MS DI AddHttpClient() approaches

**Requirements:**
- Must be a runnable `.cs` file (runfile style)
- No API keys required (to keep sample simple)
- Should demonstrate practical HttpClient patterns
- Include comments explaining the code
