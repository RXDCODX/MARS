import { Input } from "antd";
import { useEffect, useRef, useState } from "react";

import { useSiteColors } from "../../../../../shared/Utils/useSiteColors";
import { Country, getFlagPath, searchCountries } from "./flagUtils";

type FlagSelectorProperties = {
  selectedFlag: string;
  onFlagChange: (flagCode: string) => void;
  placeholder?: string;
};

const FlagSelector: React.FC<FlagSelectorProperties> = ({
  selectedFlag,
  onFlagChange,
  placeholder = "Выберите флаг",
}) => {
  const colors = useSiteColors();
  const [searchQuery, setSearchQuery] = useState("");
  const [isOpen, setIsOpen] = useState(false);
  const [filteredCountries, setFilteredCountries] = useState<Country[]>([]);
  const inputReference = useRef<HTMLInputElement>(null);

  useEffect(() => {
    setFilteredCountries(searchCountries(searchQuery));
  }, [searchQuery]);

  const handleSelect = (countryCode: string) => {
    onFlagChange(countryCode);
    setIsOpen(false);
    const selectedCountry = searchCountries(countryCode).find(
      c => c.code === countryCode
    );
    if (selectedCountry) {
      setSearchQuery(selectedCountry.name);
    }
  };

  const handleInputChange = (e: React.ChangeEvent<HTMLInputElement>) => {
    const value = e.target.value;
    setSearchQuery(value);
    setIsOpen(true);

    if (value && selectedFlag) {
      onFlagChange("");
    }
  };

  const handleInputFocus = () => {
    setIsOpen(true);
  };

  const handleInputBlur = () => {
    setTimeout(() => setIsOpen(false), 200);
  };

  const selectedCountry = selectedFlag
    ? searchCountries(selectedFlag).find(c => c.code === selectedFlag)
    : null;

  return (
    <div style={{ position: "relative", width: "100%" }}>
      <div style={{ position: "relative" }}>
        <Input
          ref={inputReference as React.RefObject<import("antd").InputRef>}
          placeholder={selectedFlag ? "" : placeholder}
          value={searchQuery}
          onChange={handleInputChange}
          onFocus={handleInputFocus}
          onBlur={handleInputBlur}
          size="small"
          style={{
            backgroundColor: colors.background.card,
            color: colors.text.primary,
            borderColor: colors.border.primary,
            paddingLeft: selectedFlag && !isOpen ? 40 : 12,
            fontWeight: 700,
            borderRadius: 12,
          }}
        />

        {selectedFlag && !isOpen && selectedCountry && (
          <div
            style={{
              position: "absolute",
              top: "50%",
              left: 12,
              transform: "translateY(-50%)",
              display: "flex",
              alignItems: "center",
              pointerEvents: "none",
              zIndex: 1,
            }}
          >
            <img
              src={getFlagPath(selectedFlag)}
              alt={selectedCountry.name}
              style={{
                width: 20,
                height: 15,
                objectFit: "cover",
                borderRadius: 2,
                border: `1px solid ${colors.border.primary}`,
              }}
              onError={e => {
                e.currentTarget.style.display = "none";
              }}
            />
          </div>
        )}
      </div>

      {isOpen && (
        <div
          style={{
            position: "absolute",
            width: "100%",
            maxHeight: 200,
            overflowY: "auto",
            zIndex: 1000,
            top: "100%",
            marginTop: 4,
            backgroundColor: colors.background.card,
            border: `1px solid ${colors.border.primary}`,
            borderRadius: 12,
            boxShadow: colors.shadow.medium,
          }}
        >
          {filteredCountries.map(country => (
            <div
              key={country.code}
              style={{
                display: "flex",
                alignItems: "center",
                gap: 8,
                padding: 8,
                cursor: "pointer",
                borderBottom: `1px solid ${colors.border.primary}`,
                color: colors.text.primary,
                transition: "background-color 0.2s ease",
              }}
              onClick={() => handleSelect(country.code)}
              onMouseEnter={e =>
                (e.currentTarget.style.backgroundColor =
                  colors.hover.background)
              }
              onMouseLeave={e =>
                (e.currentTarget.style.backgroundColor = "transparent")
              }
            >
              <img
                src={getFlagPath(country.code)}
                alt={country.name}
                style={{
                  width: 20,
                  height: 15,
                  objectFit: "cover",
                  flexShrink: 0,
                  borderRadius: 2,
                  border: `1px solid ${colors.border.primary}`,
                }}
                onError={e => {
                  e.currentTarget.style.display = "none";
                }}
              />
              <span style={{ fontSize: 12, color: colors.text.primary }}>
                {country.name}
              </span>
            </div>
          ))}
          {filteredCountries.length === 0 && (
            <div
              style={{ padding: 8, color: colors.text.secondary, fontSize: 12 }}
            >
              Страна не найдена
            </div>
          )}
        </div>
      )}
    </div>
  );
};

export default FlagSelector;
