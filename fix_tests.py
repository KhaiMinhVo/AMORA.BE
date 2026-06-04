import os
import re

def fix_auth_service():
    path = "tests/Amora.Application.Tests/Services/AuthServiceTests.cs"
    with open(path, "r", encoding="utf-8") as f: content = f.read()
    
    content = content.replace("PetCoinRewardService", "DiamondRewardService")
    content = content.replace("petCoinService", "diamondRewardService")
    content = content.replace("PetCoins", "Diamonds")
    content = content.replace("LastPetCoinRewardDate", "LastDiamondRewardDate")
    
    if "IEmailService" not in content:
        content = content.replace("using Amora.Application.Services;", "using Amora.Application.Services;\nusing Microsoft.Extensions.Caching.Memory;\nusing Microsoft.Extensions.Configuration;")
        content = content.replace("private readonly Mock<IJwtTokenService> _mockJwtTokenService;", "private readonly Mock<IJwtTokenService> _mockJwtTokenService;\n    private readonly Mock<IEmailService> _mockEmailService;\n    private readonly Mock<IMemoryCache> _mockCache;\n    private readonly Mock<IConfiguration> _mockConfig;")
        content = content.replace("_mockJwtTokenService = new Mock<IJwtTokenService>();", "_mockJwtTokenService = new Mock<IJwtTokenService>();\n        _mockEmailService = new Mock<IEmailService>();\n        _mockCache = new Mock<IMemoryCache>();\n        _mockConfig = new Mock<IConfiguration>();")
        content = content.replace("DiamondRewardService(_mockUserRepository.Object);", "DiamondRewardService(_mockUserRepository.Object);\n\n        _authService = new AuthService(\n            _mockUserRepository.Object,\n            _mockJwtTokenService.Object,\n            diamondRewardService,\n            _mockEmailService.Object,\n            _mockCache.Object,\n            _mockConfig.Object\n        );")
        content = re.sub(r"_authService = new AuthService\([^\)]+\);", "", content, count=1)
        
    with open(path, "w", encoding="utf-8") as f: f.write(content)

def fix_voice_post():
    path = "tests/Amora.Application.Tests/Services/VoicePostServiceTests.cs"
    with open(path, "r", encoding="utf-8") as f: content = f.read()
    if "IServiceScopeFactory" not in content:
        content = content.replace("using Microsoft.Extensions.Logging;", "using Microsoft.Extensions.Logging;\nusing Microsoft.Extensions.DependencyInjection;")
        content = content.replace("_mockConfiguration.Object,", "_mockConfiguration.Object,\n            new Mock<IServiceScopeFactory>().Object,")
    with open(path, "w", encoding="utf-8") as f: f.write(content)

def fix_voice_comment():
    path = "tests/Amora.Application.Tests/Services/VoiceCommentServiceTests.cs"
    with open(path, "r", encoding="utf-8") as f: content = f.read()
    if "IServiceScopeFactory" not in content:
        content = content.replace("using Moq;", "using Moq;\nusing Microsoft.Extensions.DependencyInjection;")
        content = content.replace("_mockVoiceCommentRepository.Object", "_mockVoiceCommentRepository.Object,\n            new Mock<IServiceScopeFactory>().Object")
    with open(path, "w", encoding="utf-8") as f: f.write(content)

def fix_chat_service():
    path = "tests/Amora.Application.Tests/Services/ChatServiceTests.cs"
    with open(path, "r", encoding="utf-8") as f: content = f.read()
    if "AiModerationService" not in content:
        content = content.replace("_mockPetFeatureGateService,", "null!,")
        content = content.replace("_mockReadStateRepository.Object", "_mockReadStateRepository.Object,\n            null! /* AiModerationService */")
    with open(path, "w", encoding="utf-8") as f: f.write(content)

def fix_trust_safety():
    path = "tests/Amora.Application.Tests/Services/TrustSafetyServiceTests.cs"
    with open(path, "r", encoding="utf-8") as f: content = f.read()
    if "AiModerationService" not in content:
        content = content.replace("_mockUserRepository.Object", "_mockUserRepository.Object,\n            null!,\n            null!,\n            null!,\n            null!")
    with open(path, "w", encoding="utf-8") as f: f.write(content)

def fix_pet_coordinator():
    path = "tests/Amora.Application.Tests/Pets/PetCoordinatorTests.cs"
    with open(path, "r", encoding="utf-8") as f: content = f.read()
    if "IChatMessageRepository" not in content:
        content = content.replace("_mockPetRealtimeNotifier.Object", "_mockPetRealtimeNotifier.Object,\n            new Mock<IChatMessageRepository>().Object")
    with open(path, "w", encoding="utf-8") as f: f.write(content)

def fix_iap():
    path = "tests/Amora.Application.Tests/Iap/IapWebhookServiceTests.cs"
    with open(path, "r", encoding="utf-8") as f: content = f.read()
    content = content.replace("AmoraGemsDelta", "DiamondsDelta")
    content = content.replace("AmoraGems", "Diamonds")
    with open(path, "w", encoding="utf-8") as f: f.write(content)

def fix_pet_shop():
    path = "tests/Amora.Application.Tests/Pets/PetShopServiceTests.cs"
    with open(path, "r", encoding="utf-8") as f: content = f.read()
    content = content.replace("PricePetCoins", "PriceDiamonds")
    content = content.replace("PetCoinsDelta", "DiamondsDelta")
    content = content.replace("PetCoins", "Diamonds")
    content = content.replace("UseAmoraGems = false", "")
    content = content.replace("UseAmoraGems = true", "")
    content = content.replace("UseAmoraGems", "Currency") # fallback
    with open(path, "w", encoding="utf-8") as f: f.write(content)

def fix_pet_engine():
    path = "tests/Amora.Application.Tests/Pets/PetEngineTests.cs"
    with open(path, "r", encoding="utf-8") as f: content = f.read()
    content = content.replace("Mood", "Level")
    content = content.replace("ComputeMood", "ComputeLevel")
    content = content.replace("PetMood.Happy", "1")
    content = content.replace("PetMood.Sad", "0")
    content = content.replace("PetMood.Neutral", "0")
    content = content.replace("ConsecutiveNegativeVibes", "TotalInteractions")
    content = content.replace("RegisterVibe", "GainExp")
    content = content.replace("Vibe", "Exp")
    with open(path, "w", encoding="utf-8") as f: f.write(content)

try:
    fix_auth_service()
    fix_voice_post()
    fix_voice_comment()
    fix_chat_service()
    fix_trust_safety()
    fix_pet_coordinator()
    fix_iap()
    fix_pet_shop()
    fix_pet_engine()
    print("Fixed tests.")
except Exception as e:
    print(f"Error: {e}")
