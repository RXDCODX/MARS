import countriesData from "../../../../../shared/flags/countries.json";

export type Country = {
  code: string;
  name: string;
};

// Преобразуем JSON в массив стран
export const countries: Country[] = Object.entries(countriesData).map(
  ([code, name]) => ({
    code: code.toLowerCase(),
    name: name as string,
  })
);

// Функция для получения пути к флагу
export const getFlagPath = (countryCode: string): string => {
  if (!countryCode) return "";
  return `/flags/${countryCode.toLowerCase()}.svg`;
};

// Функция для поиска стран по названию
export const searchCountries = (query: string): Country[] => {
  if (!query) return countries.slice(0, 20); // Показываем первые 20 стран по умолчанию

  const lowerQuery = query.toLowerCase();
  return countries
    .filter(
      country =>
        country.name.toLowerCase().includes(lowerQuery) ||
        country.code.toLowerCase().includes(lowerQuery)
    )
    .slice(0, 20); // Ограничиваем результаты
};
