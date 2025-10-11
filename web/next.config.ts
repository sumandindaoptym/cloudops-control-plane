import type { NextConfig } from "next";

const nextConfig: NextConfig = {
  serverExternalPackages: ['@node-rs/argon2', '@node-rs/bcrypt'],
  webpack: (config) => {
    config.externals.push('@node-rs/argon2', '@node-rs/bcrypt');
    return config;
  },
};

export default nextConfig;
