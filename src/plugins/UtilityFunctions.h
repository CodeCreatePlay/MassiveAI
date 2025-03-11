#include <vector>
#include <stdexcept>
#include <numeric>

extern "C" __declspec(dllexport) float CalculateUtility(const float* factors, const float* weights, size_t length) {
    if (factors == nullptr || weights == nullptr || length == 0) {
        throw std::invalid_argument("Factors and weights arrays must not be null and must have a positive length.");
    }

    float weightedSum = 0.0f;
    float weightTotal = 0.0f;

    for (size_t i = 0; i < length; ++i) {
        weightedSum += factors[i] * weights[i];
        weightTotal += weights[i];
    }

    if (weightTotal == 0.0f) {
        return 0.0f; // Avoid division by zero, return minimum utility.
    }

    return weightedSum / weightTotal; // Return normalized utility score.
}
